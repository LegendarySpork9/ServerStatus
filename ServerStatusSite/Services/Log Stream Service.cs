// Copyright © - Unpublished - Toby Hunter
using ServerStatusSite.Models.Responses.Related;
using System.Collections.Concurrent;

namespace ServerStatusSite.Services
{
    public class LogStreamService
    {
        private readonly ConcurrentDictionary<string, List<(Func<List<LogEntryModel>, Task> Handler, string? WebhookId)>> _Subscribers = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _ActiveWebhookIds = new();

        /// <summary>
        /// Subscribes a handler to receive logs for the given server.
        /// </summary>
        public void Subscribe(
            string serverName,
            Func<List<LogEntryModel>, Task> handler,
            string? webhookId = null)
        {
            _Subscribers.AddOrUpdate(
                serverName,
                _ => [(handler, webhookId)],
                (_, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add((
                            handler,
                            webhookId));
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
                out List<(Func<List<LogEntryModel>, Task> Handler, string? WebhookId)>? handlers))
            {
                lock (handlers)
                {
                    handlers.RemoveAll(s => s.Handler == handler);
                }
            }
        }

        /// <summary>
        /// Registers a webhook ID as active.
        /// </summary>
        public void RegisterWebhookId(string webhookId)
        {
            _ActiveWebhookIds.TryAdd(
                webhookId,
                0);
        }

        /// <summary>
        /// Unregisters a webhook ID.
        /// </summary>
        public void UnregisterWebhookId(string webhookId)
        {
            _ActiveWebhookIds.TryRemove(
                webhookId,
                out _);
        }

        /// <summary>
        /// Checks whether a webhook ID is currently active.
        /// </summary>
        public bool IsWebhookActive(string webhookId)
        {
            return _ActiveWebhookIds.ContainsKey(webhookId);
        }

        /// <summary>
        /// Publishes logs to all subscribers for the given server.
        /// Returns a list of orphaned webhook IDs from failed handlers.
        /// </summary>
        public async Task<List<string>> Publish(
            string serverName,
            List<LogEntryModel> logs)
        {
            List<string> orphanedWebhookIds = [];

            if (_Subscribers.TryGetValue(
                serverName,
                out List<(Func<List<LogEntryModel>, Task> Handler, string? WebhookId)>? handlers))
            {
                (Func<List<LogEntryModel>, Task> Handler, string? WebhookId)[] snapshot;

                lock (handlers)
                {
                    snapshot = [.. handlers];
                }

                foreach (var (handler, webhookId) in snapshot)
                {
                    try
                    {
                        await handler(logs);
                    }

                    catch
                    {
                        lock (handlers)
                        {
                            handlers.RemoveAll(s => s.Handler == handler);
                        }

                        if (webhookId != null)
                        {
                            UnregisterWebhookId(webhookId);
                            orphanedWebhookIds.Add(webhookId);
                        }
                    }
                }
            }

            return orphanedWebhookIds;
        }
    }
}
