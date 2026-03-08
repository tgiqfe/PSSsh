
namespace PSSsh.Lib
{
    internal class ServerInfo
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; }
        public string URI { get { return $"{this.Protocol}://{this.Server}:{this.Port}"; } }

        public ServerInfo() { }

        public ServerInfo(string uri)
        {
            ReadURI(uri);
        }

        public ServerInfo(string uri, int defaultPort, string defaultProtocol)
        {
            ReadURI(uri);
            if (Port == 0) { Port = defaultPort; }
            if (string.IsNullOrEmpty(Protocol)) { Protocol = defaultProtocol.ToLower(); }
        }

        /// <summary>
        /// URIからサーバアドレス(IP or FQDN)、ポート、プロトコルを格納。
        /// </summary>
        /// <param name="uri"></param>
        private void ReadURI(string uri)
        {
            string tempServer = uri;
            string tempPort = "0";
            string tempProtocol = "";

            System.Text.RegularExpressions.Match match;
            if ((match = System.Text.RegularExpressions.Regex.Match(tempServer, "^.+(?=://)")).Success)
            {
                tempProtocol = match.Value;
                tempServer = tempServer.Substring(tempServer.IndexOf("://") + 3);
            }
            if ((match = System.Text.RegularExpressions.Regex.Match(tempServer, @"(?<=:)\d+")).Success)
            {
                tempPort = match.Value;
                tempServer = tempServer.Substring(0, tempServer.IndexOf(":"));
            }

            Server = tempServer;
            Port = int.Parse(tempPort);
            Protocol = tempProtocol.ToLower();
        }
    }
}
