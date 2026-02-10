using System.Collections.Concurrent;

namespace FarmServer
{
    public class SessionManager
    {
        // Thread-safe dictionary to store active sessions
        private readonly ConcurrentDictionary<string, Session> _sessions = new();

        public Session? GetSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return null;
            
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Touch();
                return session;
            }
            return null;
        }

        public Session CreateSession()
        {
            var sessionId = SecurityUtils.GenerateSessionId();
            var context = new GameContext();
            var session = new Session(sessionId, context);
            
            _sessions.TryAdd(sessionId, session);
            return session;
        }
    }
}
