using System;
using UnityEngine;
namespace Mirror
{
	public class BuletInterpolation : NetworkBehaviour
	{
		[Header("Authority")]
		[Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
		public bool clientAuthority;

		/// <summary>
		/// We need to store this locally on the server so clients can't request Authority when ever they like
		/// </summary>
		bool clientAuthorityBeforeTeleport;

		// Is this a client with authority over this transform?
		// This component could be on the player object or any object that has been assigned authority to this client.
		bool IsClientWithAuthority => hasAuthority && clientAuthority;

		// Sensitivity is added for VR where human players tend to have micro movements so this can quiet down
		// the network traffic.  Additionally, rigidbody drift should send less traffic, e.g very slow sliding / rolling.
		[Header("Sensitivity")]
		[Tooltip("Changes to the transform must exceed these values to be transmitted on the network.")]
		public float localPositionSensitivity = .01f;

		[Header("Interpolation")]
		[Tooltip("Set to true if position should be interpolated, false is ideal for grid bassed movement")]
		public bool interpolatePosition = true;

		// target transform to sync. can be on a child.
		public Transform targetComponent;

		// server
		Vector3 lastPosition;

		// client
		public class DataPoint
		{
			public float timeStamp;
			public Vector3 localPosition;
			public float movementSpeed;
		}
		// interpolation start and goal
		DataPoint start;
		DataPoint goal;

		float lastClientSendTime;

		public static void SerializeIntoWriter(NetworkWriter writer, Vector3 position)
		{
			writer.WriteVector3(position);
		}

		public override bool OnSerialize(NetworkWriter writer, bool initialState)
		{
			SerializeIntoWriter(writer, targetComponent.localPosition);
			return true;
		}

		static float EstimateMovementSpeed(DataPoint from, DataPoint to, Transform transform, float sendInterval)
		{
			Vector3 delta = to.localPosition - (from != null ? from.localPosition : transform.localPosition);
			float elapsed = from != null ? to.timeStamp - from.timeStamp : sendInterval;
			// avoid NaN
			return elapsed > 0 ? delta.magnitude / elapsed : 0;
		}

		// serialization is needed by OnSerialize and by manual sending from authority
		void DeserializeFromReader(NetworkReader reader)
		{
			DataPoint temp = new DataPoint
			{
				localPosition = reader.ReadVector3(),
				timeStamp = Time.time
			};
			temp.movementSpeed = EstimateMovementSpeed(goal, temp, targetComponent, syncInterval);

			if (start == null)
			{
				start = new DataPoint
				{
					timeStamp = Time.time - syncInterval,
					localPosition = targetComponent.localPosition,
					movementSpeed = temp.movementSpeed
				};
			}
			// -> second or nth data point? then update previous, but:
			//    we start at where ever we are right now, so that it's
			//    perfectly smooth and we don't jump anywhere
			//
			//    example if we are at 'x':
			//
			//        A--x->B
			//
			//    and then receive a new point C:
			//
			//        A--x--B
			//              |
			//              |
			//              C
			//
			//    then we don't want to just jump to B and start interpolation:
			//
			//              x
			//              |
			//              |
			//              C
			//
			//    we stay at 'x' and interpolate from there to C:
			//
			//           x..B
			//            \ .
			//             \.
			//              C
			//
			else
			{
				float oldDistance = Vector3.Distance(start.localPosition, goal.localPosition);
				float newDistance = Vector3.Distance(goal.localPosition, temp.localPosition);

				start = goal;

				if (Vector3.Distance(targetComponent.localPosition, start.localPosition) < oldDistance + newDistance)
				{
					start.localPosition = targetComponent.localPosition;

				}
			}
			goal = temp;
		}

		public override void OnDeserialize(NetworkReader reader, bool initialState)
		{
			// deserialize
			DeserializeFromReader(reader);
		}

		// local authority client sends sync message to server for broadcasting
		[Command(channel = Channels.Unreliable)]
		void CmdClientToServerSync(ArraySegment<byte> payload)
		{
			// Ignore messages from client if not in client authority mode
			if (!clientAuthority)
				return;

			using (PooledNetworkReader networkReader = NetworkReaderPool.GetReader(payload))
				DeserializeFromReader(networkReader);

			if (isServer && !isClient)
				ApplyPosition(goal.localPosition);

			SetDirtyBit(1UL);
		}

		Vector3 InterpolatePosition(DataPoint start, DataPoint goal, Vector3 currentPosition)
		{
			if (!interpolatePosition)
			{
				return goal.localPosition;
			}
			else if (start != null)
			{
				float speed = Mathf.Max(start.movementSpeed, goal.movementSpeed);
				return Vector3.MoveTowards(currentPosition, goal.localPosition, speed * Time.deltaTime);
			}
			return currentPosition;
		}
		bool NeedsTeleport()
		{
			// calculate time between the two data points
			float startTime = start != null ? start.timeStamp : Time.time - syncInterval;
			float goalTime = goal != null ? goal.timeStamp : Time.time;
			float difference = goalTime - startTime;
			float timeSinceGoalReceived = Time.time - goalTime;
			return timeSinceGoalReceived > difference * 5;
		}

		bool HasMoved()
		{
			bool moved = Vector3.Distance(lastPosition, targetComponent.localPosition) > localPositionSensitivity;
			bool change = moved;
			if (change)
			{
				lastPosition = targetComponent.localPosition;
			}
			return change;
		}

		void ApplyPosition(Vector3 position)
		{
			targetComponent.localPosition = position;
		}

		void Update()
		{
			if (isServer)
			{
				SetDirtyBit(HasMoved() ? 1UL : 0UL);
			}

			// no 'else if' since host mode would be both
			if (isClient)
			{
				if (!isServer && IsClientWithAuthority)
				{
					// check only each 'syncInterval'
					if (Time.time - lastClientSendTime >= syncInterval)
					{
						if (HasMoved())
						{
							using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
							{
								SerializeIntoWriter(writer, targetComponent.localPosition);

								CmdClientToServerSync(writer.ToArraySegment());
							}
						}
						lastClientSendTime = Time.time;
					}
				}

				if (!IsClientWithAuthority)
				{
					// received one yet? (initialized?)
					if (goal != null)
					{
						// teleport or interpolate
						if (NeedsTeleport())
						{
							ApplyPosition(goal.localPosition);

							start = null;
							goal = null;
						}
						else
						{
							ApplyPosition(InterpolatePosition(start, goal, targetComponent.localPosition));
						}
					}
				}
			}
		}

	
		[Server]
		public void ServerTeleport(Vector3 position)
		{
			Quaternion rotation = transform.rotation;
			ServerTeleport(position, rotation);
		}
	
		[Server]
		public void ServerTeleport(Vector3 position, Quaternion rotation)
		{
			clientAuthorityBeforeTeleport = clientAuthority || clientAuthorityBeforeTeleport;
			clientAuthority = false;

			DoTeleport(position);

			RpcTeleport(position, clientAuthorityBeforeTeleport);
		}

		void DoTeleport(Vector3 newPosition)
		{
			transform.position = newPosition;

			goal = null;
			start = null;
			lastPosition = newPosition;
		}

		[ClientRpc]
		void RpcTeleport(Vector3 newPosition, bool isClientAuthority)
		{
			DoTeleport(newPosition);

			if (hasAuthority && isClientAuthority)
				CmdTeleportFinished();
		}
		[Command]
		void CmdTeleportFinished()
		{
			if (clientAuthorityBeforeTeleport)
			{
				clientAuthority = true;

				// reset value so doesn't effect future calls, see note in ServerTeleport
				clientAuthorityBeforeTeleport = false;
			}
			else
			{
				Debug.LogWarning("Client called TeleportFinished when clientAuthority was false on server", this);
			}
		}

		static void DrawDataPointGizmo(DataPoint data, Color color)
		{

			Vector3 offset = Vector3.up * 0.01f;

			Gizmos.color = color;
			Gizmos.DrawSphere(data.localPosition + offset, 0.5f);
		}

		static void DrawLineBetweenDataPoints(DataPoint data1, DataPoint data2, Color color)
		{
			Gizmos.color = color;
			Gizmos.DrawLine(data1.localPosition, data2.localPosition);
		}

		// draw the data points for easier debugging
		void OnDrawGizmos()
		{
			// draw start and goal points
			if (start != null)
				DrawDataPointGizmo(start, Color.gray);

			if (goal != null)
				DrawDataPointGizmo(goal, Color.white);

			// draw line between them
			if (start != null && goal != null)
				DrawLineBetweenDataPoints(start, goal, Color.cyan);
		}
	}
}
