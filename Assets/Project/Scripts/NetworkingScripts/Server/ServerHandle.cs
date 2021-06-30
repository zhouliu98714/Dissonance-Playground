using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CEMSIM.GameLogic;

namespace CEMSIM
{
    namespace Network
    {
        public class ServerHandle : MonoBehaviour
        {
            public static void InvalidPacketResponse(int _fromClient, Packet _packet)
            {
                Debug.LogWarning($"Client {_fromClient} sends an invalid packet");
                NetworkOverlayMenu.Instance.Log($"Client {_fromClient} sends an invalid packet");
                return;
            }

            
            public static void Welcome(int _fromClient, Packet _packet)
            {
                // Do nothing, because the "Welcome" packet is the first packet sent by the client through UDP
                // It is used to verify the establishment of UDP connection
            }

            public static void WelcomeReceived(int _fromClient, Packet _packet)
            {
                int _clientIdCheck = _packet.ReadInt32();
                string _username = _packet.ReadString();

                Debug.Log($"Client {ServerInstance.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connects successfully and whose username is {_username}");
                NetworkOverlayMenu.Instance.Log($"Client {ServerInstance.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connects successfully and whose username is {_username}");

                // check whether the packet is from the client
                if (_clientIdCheck != _fromClient)
                {
                    Debug.LogWarning($"Client {_fromClient} has assumed with client id {_clientIdCheck} with username {_username}");
                    NetworkOverlayMenu.Instance.Log($"Warning: Client {_fromClient} has assumed with client id {_clientIdCheck} with username {_username}");
                    return;
                }

            }

            public static void PingUDP(int _fromClient, Packet _packet)
            {
                // Digest the packet
                int _clientIdCheck = _packet.ReadInt32();
                string _msg = _packet.ReadString();

                Debug.Log($"Client {ServerInstance.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} sends a UDP ping with msg {_msg}");
                NetworkOverlayMenu.Instance.Log($"Client {ServerInstance.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} sends a UDP ping with msg {_msg}");

                // check whether the packet is from the client
                if (_clientIdCheck != _fromClient)
                {
                    Debug.Log($"Client {_fromClient} has assumed with client id {_clientIdCheck} ");
                    NetworkOverlayMenu.Instance.Log($"Client {_fromClient} has assumed with client id {_clientIdCheck} ");
                    return;
                }

                // Create response
                // we reply the client with the same mesage appended with a check message
                string _replyMsg = _msg + " - server read";
                ServerSend.UDPPingReply(_fromClient, _msg);
            }


            public static void PingTCP(int _fromClient, Packet _packet)
            {
                // Digest the packet
                string _msg = _packet.ReadString();

                Debug.Log($"Client {ServerInstance.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} sends a TCP ping with msg {_msg}");
                NetworkOverlayMenu.Instance.Log($"Client {ServerInstance.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} sends a TCP ping with msg {_msg}");


                // Create response
                // we reply the client with the same mesage appended with a check message
                string _replyMsg = _msg + " - server read";
                ServerSend.TCPPingReply(_fromClient, _msg);
            }

            /// <summary>
            /// In response to client's SpawnRequest packet.
            /// Send the player into the game (simulation) and reply with the spawn detail
            /// </summary>
            /// <param name="_fromClient"></param>
            /// <param name="_packet"></param>
            public static void SpawnRequest(int _fromClient, Packet _packet)
            {
                string _username = _packet.ReadString();
                bool _vr = _packet.ReadBool();
                int _role_i = _packet.ReadInt32();

                Debug.Log($"client{_fromClient}: Spawn player.");
                NetworkOverlayMenu.Instance.Log($"client{_fromClient}: Spawn player.");

                // send back the packet with necessary inforamation about player locations
                ServerInstance.clients[_fromClient].SendIntoGame(_username, _vr, _role_i);
            }

            /// <summary>
            /// Handle the user control on the player and respond with the updated player status
            /// </summary>
            /// <param name="_fromClient"></param>
            /// <param name="_packet"></param>
            public static void PlayerDesktopMovement(int _fromClient, Packet _packet)
            {
                bool[] _inputs = new bool[_packet.ReadInt32()];
                for (int i = 0; i < _inputs.Length; i++)
                {
                    _inputs[i] = _packet.ReadBool();
                }

                Quaternion _rotation = _packet.ReadQuaternion();

                //Debug.Log($"client{_fromClient}: move packet received.");
                ServerPlayerDesktop fromPlayer = (ServerPlayerDesktop)ServerInstance.clients[_fromClient].player;
                fromPlayer.SetInput(_inputs, _rotation);
            }

            /// <summary>
            /// Handle the VR position and orientation
            /// </summary>
            /// <param name="_fromClient"></param>
            /// <param name="_packet"></param>
            public static void PlayerVRMovement(int _fromClient, Packet _packet)
            {
                // avatar position
                Vector3 _position = _packet.ReadVector3();
                Quaternion _rotation = _packet.ReadQuaternion();

                // left and right controller positions
                Vector3 _leftPosition = _packet.ReadVector3();
                Quaternion _leftRotation = _packet.ReadQuaternion();
                Vector3 _rightPosition = _packet.ReadVector3();
                Quaternion _rightRotation = _packet.ReadQuaternion();

                //Debug.Log($"client{_fromClient}: move packet received.");
                ServerPlayerVR fromPlayer = (ServerPlayerVR)ServerInstance.clients[_fromClient].player;
                fromPlayer.SetPosition(_position, _rotation);
                fromPlayer.SetControllerPositions(_leftPosition, _leftRotation, _rightPosition, _rightRotation);
            }

            // update the TCP round-trip-time based on the response packet
            public static void HeartBeatDetectionTCP(int _fromClient, Packet _packet)
            {
                long utcnow = System.DateTime.UtcNow.Ticks;
                long sendticks = _packet.ReadInt64();
                ServerInstance.clients[_fromClient].tcp.lastHeartBeat = utcnow;
                ServerInstance.clients[_fromClient].tcp.rtt = utcnow - sendticks;
            }

            // update the UDP round-trip-time based on the response packet
            public static void HeartBeatDetectionUDP(int _fromClient, Packet _packet)
            {
                long utcnow = System.DateTime.UtcNow.Ticks;
                long sendticks = _packet.ReadInt64();
                ServerInstance.clients[_fromClient].udp.lastHeartBeat = utcnow;
                ServerInstance.clients[_fromClient].udp.rtt = utcnow - sendticks;
            }


            /// <summary>
            /// Update an item's position as instructed in packet
            /// </summary>
            /// <param name="_packet"></param>
            public static void ItemPosition(int _fromClient, Packet _packet)
            {
                // interpret the packet
                int _item_id = _packet.ReadInt32();
                Vector3 _position = _packet.ReadVector3();
                Quaternion _rotation = _packet.ReadQuaternion();


                // Update item position
                GameObject itemManager = GameObject.Find("ItemManager");
                ServerItemManager SIM = (ServerItemManager)itemManager.GetComponent(typeof(ServerItemManager));
                //Ignore if the client is not the owner of the item
                if (SIM.itemList[_item_id].GetComponent<ItemController>().ownerId != _fromClient){   
                    Debug.Log(string.Format("client {0} attempted to update pos on item {1} but ignored by server",_fromClient,_item_id));
                    return;
                }
                SIM.UpdateItemPosition(_item_id, _position, _rotation);
            }

            /// <summary>
            /// Update an item's ownership as instructed in packet
            /// </summary>
            /// <param name="_packet"></param>
            public static void ItemOwnershipChange(int _fromClient, Packet _packet)
            {
                int _item_id = _packet.ReadInt32();
                int _newOwner = _packet.ReadInt32();
                GameObject itemManager = GameObject.Find("ItemManager");
                ServerItemManager SIM = (ServerItemManager)itemManager.GetComponent(typeof(ServerItemManager));

                GameObject item = SIM.itemList[_item_id];
                ItemController itemCon = item.GetComponent<ItemController>();
                int currentOwner = item.GetComponent<ItemController>().ownerId;
                //This item is currently not owned by anyone\ or owned by the incoming client
                if (currentOwner == 0 || currentOwner == _fromClient){ 
                    itemCon.ownerId = _newOwner;
                    Rigidbody rb = item.GetComponent<Rigidbody>(); 
                    if(_newOwner != 0){ 
                        //if the item is no longer controlled by server then set item to kinematic and no gravity
                        rb.isKinematic = true;                  //Prevent server physics system from changing the item's position & rotation
                        rb.useGravity = false;
                    }else{
                        //if server regains control of an item then turn on gravity and set kinematic off
                        rb.isKinematic = false;                  
                        rb.useGravity = true;

                    }
                    Debug.Log(string.Format("Ownership of item {0} is given to player {1}.",_item_id.ToString(),_newOwner));
                }
                //If this item is currenly owned by other clients
                if (currentOwner !=0 && currentOwner != _fromClient){
                    //Makes no change in ownership and reply with denial
                    ServerSend.OwnershipDenial(_fromClient, _item_id);
                    Debug.Log("Ownership denied.");
                }
            }


            public static void EnvironmentState(int _fromClient, Packet _packet)
            {
                int _eventId = _packet.ReadInt32();
                ServerNetworkManager.handleEventPacket(_fromClient, _eventId, _packet);
            }


        }
    }
}
