// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Models.Responses.Related;
using System.Collections.Concurrent;

namespace ServerStatusSite.Services
{
    public class LogStreamService
    {
        private readonly ConcurrentDictionary<string, List<Func<List<LogEntryModel>, Task>>> _Subscribers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Subscribes a handler to receive logs for the given server.
        /// </summary>
        public void Subscribe(
            string serverName,
            Func<List<LogEntryModel>, Task> handler)
        {
            _Subscribers.AddOrUpdate(
                serverName,
                _ => [handler],
                (_, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(handler);
                    }

                    return existing;
                });
        }

        /// <summary>
        /// Unsubscribes a handler from receiving logs for the given server.
        /// </summary>
        public void Unsubscribe(
            string serverName,
            Func<List<LogEntryModel>, Task> handler)
        {
            if (_Subscribers.TryGetValue(
                serverName,
                out List<Func<List<LogEntryModel>, Task>>? handlers))
            {
                lock (handlers)
                {
                    handlers.Remove(handler);
                }
            }
        }

        /// <summary>
        /// Publishes logs to all subscribers for the given server.
        /// </summary>
        public async Task Publish(
            string serverName,
            List<LogEntryModel> logs)
        {
            if (_Subscribers.TryGetValue(
                serverName,
                out List<Func<List<LogEntryModel>, Task>>? handlers))
            {
                Func<List<LogEntryModel>, Task>[] snapshot;

                lock (handlers)
                {
                    snapshot = [.. handlers];
                }

                foreach (Func<List<LogEntryModel>, Task> handler in snapshot)
                {
                    await handler(logs);
                }
            }
        }
    }
}
