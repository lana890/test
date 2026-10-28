using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Core.Hubs
{
    public class AlertTracker
    {
        private static readonly Dictionary<long, List<string>> OnlineUsers = new();

        public Task<bool> UserConnected(long UserId, string connectionId)
        {
            
            bool isOnline = false;
            lock (OnlineUsers)
            {
                if (OnlineUsers.ContainsKey(UserId))
                {
                    OnlineUsers[UserId].Add(connectionId);
                }
                else
                {
                    OnlineUsers.Add(UserId, new List<string> { connectionId });
                    isOnline = true;
                }
            }

            return Task.FromResult(isOnline);
        }

        public Task<bool> UserDisconnected(long UserId, string connectionId)
        {
            bool isOffline = false;
            lock (OnlineUsers)
            {
                if (!OnlineUsers.ContainsKey(UserId)) return Task.FromResult(isOffline);

                OnlineUsers[UserId].Remove(connectionId);

                if (OnlineUsers[UserId].Count == 0)
                {
                    OnlineUsers.Remove(UserId);
                    isOffline = true;
                }
            }

            return Task.FromResult(isOffline);
        }


        public static Task<List<string>> GetConnectionsForUser(long UserId)
        {
            List<string> connectionIds;

            lock (OnlineUsers)
            {
                connectionIds = OnlineUsers.GetValueOrDefault(UserId);
            }

            return Task.FromResult(connectionIds);
        }
}
    }