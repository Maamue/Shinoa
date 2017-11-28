﻿// <copyright file="ImageSpamService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Databases;

    using Discord;
    using Discord.Net;
    using Discord.WebSocket;

    using Microsoft.EntityFrameworkCore;

    using TimedServices;

    using static Databases.ImageSpamContext;

    public class ImageSpamService : IDatabaseService
    {
        private DbContextOptions dbOptions;
        private ModerationService moderationService;

        public async Task<bool> AddBinding(IMessageChannel channel)
        {
            using (var db = new ImageSpamContext(dbOptions))
            {
                if (db.ImageSpamBindings.Any(b => b.ChannelId == channel.Id)) return false;

                db.ImageSpamBindings.Add(new ImageSpamBinding
                {
                    ChannelId = channel.Id,
                });
                await db.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> RemoveBinding(IEntity<ulong> binding)
        {
            using (var db = new ImageSpamContext(dbOptions))
            {
                var entities = db.ImageSpamBindings.Where(b => b.ChannelId == binding.Id);
                if (!entities.Any()) return false;

                db.ImageSpamBindings.RemoveRange(entities);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public bool CheckBinding(IMessageChannel channel)
        {
            using (var db = new ImageSpamContext(dbOptions))
            return db.ImageSpamBindings.Any(b => b.ChannelId == channel.Id);
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            dbOptions = map.GetService(typeof(DbContextOptions)) as DbContextOptions ?? throw new ServiceNotFoundException("Database Options were not found in service provider.");
            moderationService = map.GetService(typeof(ModerationService)) as ModerationService ?? throw new ServiceNotFoundException("Moderation Service was not found in service provider.");

            var client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Discord Client was not found in service provider.");
            client.MessageReceived += Handler;
        }

        private async Task Handler(SocketMessage msg)
        {
            try
            {
                using (var db = new ImageSpamContext(dbOptions))
                {
                    if (msg.Author is IGuildUser user &&
                        !user.IsBot &&
                        await db.ImageSpamBindings.FindAsync(msg.Channel.Id.ToString()) == null &&
                        (msg.Attachments.Count > 0 || msg.Embeds.Count > 0))
                    {
                        var messages = await msg.Channel.GetMessagesAsync(limit: 50).Flatten();
                        var imagesCounter = (from message in messages.ToList().OrderByDescending(o => o.Timestamp)
                                             let timeDifference = DateTimeOffset.Now - message.Timestamp
                                             where timeDifference.TotalSeconds < 15 && message.Attachments.Count + message.Embeds.Count > 0 && message.Author.Id == msg.Author.Id
                                             select message).Sum(message => message.Attachments.Count + message.Embeds.Count);

                        if (imagesCounter > 3)
                        {
                            await msg.DeleteAsync();
                            await msg.Channel.SendMessageAsync($"{user.Mention} Your message has been removed for being image spam. You have been preemptively muted for 5 minutes.");

                            await moderationService.AddMute(user, DateTime.UtcNow + TimeSpan.FromMinutes(5));
                        }
                    }
                }
            }
            catch (HttpException)
            {
            }
        }
    }
}
