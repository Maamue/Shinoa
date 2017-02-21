﻿namespace Shinoa
{
    using System;
    using Discord;
    using Discord.Commands;

    /// <summary>
    /// Niceties for logging of commands and errors.
    /// </summary>
    public static class Logging
    {
        private static ITextChannel loggingChannel;

        /// <summary>
        /// Logs a specific string, as given in message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static async void Log(string message)
        {
            PrintWithTime(message);
            var sendMessageAsync = loggingChannel?.SendMessageAsync(message);
            if (sendMessageAsync != null) await sendMessageAsync;
        }

        /// <summary>
        /// Logs a specific string, as given in message, as an error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static async void LogError(string message)
        {
            PrintErrorWithTime(message);
            var embed = new EmbedBuilder
            {
                Title = "Error",
                Color = new Color(200, 0, 0),
                Description = message,
                Author =
                    new EmbedAuthorBuilder()
                    {
                        IconUrl = Shinoa.DiscordClient.CurrentUser.AvatarUrl,
                        Name = nameof(Shinoa),
                    },
                Timestamp = DateTimeOffset.Now,
                Footer = new EmbedFooterBuilder()
                {
                    Text = Shinoa.VersionString,
                },
            };
            var sendEmbedAsync = loggingChannel?.SendEmbedAsync(embed);
            if (sendEmbedAsync != null) await sendEmbedAsync;
        }

        /// <summary>
        /// Logs a specific Discord message as specified by the CommandContext.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        public static void LogMessage(CommandContext context)
        {
            Log(!context.IsPrivate
                ? $"[{context.Guild.Name} -> #{context.Channel.Name}] {context.User.Username}: {context.Message.Content}"
                : $"[PM] {context.User.Username}: {context.Message.Content}");
        }

        /// <summary>
        /// Initialises logging to a specific Discord Channel.
        /// </summary>
        public static void InitLoggingToChannel()
        {
            var loggingChannelId = Shinoa.Config["logging_channel_id"];
            loggingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(loggingChannelId));

            loggingChannel.SendMessageAsync("Logging initialized.");
        }

        private static void PrintWithTime(string line)
        {
            Console.WriteLine($"[{DateTime.Now.Hour:D2}:{DateTime.Now.Minute:D2}:{DateTime.Now.Second:D2}] {line}");
        }

        private static void PrintErrorWithTime(string line)
        {
            Console.Error.WriteLine($"[{DateTime.Now.Hour:D2}:{DateTime.Now.Minute:D2}:{DateTime.Now.Second:D2}] {line}");
        }
    }
}
