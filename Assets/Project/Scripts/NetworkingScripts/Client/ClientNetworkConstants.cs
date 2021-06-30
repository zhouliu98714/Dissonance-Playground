using System;

namespace CEMSIM
{
    namespace Network
    {
        public class ClientNetworkConstants
        {
            public const string SERVER_IP = "127.0.0.1";
            public const int SERVER_PORT = 54321;
            public const int DATA_BUFFER_SIZE = 4096; //4KB for both Tx & Rx
        }
    }
}