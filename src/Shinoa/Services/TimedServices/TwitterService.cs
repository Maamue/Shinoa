﻿// <copyright file="TwitterService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

using Microsoft.EntityFrameworkCore;

namespace Shinoa.Services.TimedServices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Attributes;

    using BoxKite.Twitter;
    using BoxKite.Twitter.Models;

    using Databases;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using static Databases.TwitterContext;

    [Config("twitter")]
    public class TwitterService : IDatabaseService, ITimedService
    {
        private DbContextOptions dbOptions;
        private DiscordSocketClient client;
        private ApplicationSession twitterSession;

        public Color ModuleColor { get; private set; }

        public bool AddBinding(string username, IMessageChannel channel)
        {
            using (var db = new TwitterContext(dbOptions))
            {
                var twitterBinding = new TwitterBinding
                {
                    TwitterUsername = username,
                    LatestPost = DateTime.UtcNow,
                };

                if (db.TwitterChannelBindings.Any(b => b.ChannelId == channel.Id && b.TwitterBinding.TwitterUsername == twitterBinding.TwitterUsername)) return false;

                db.TwitterChannelBindings.Add(new TwitterChannelBinding
                {
                    TwitterBinding = twitterBinding,
                    ChannelId = channel.Id,
                });

                db.SaveChanges();
                return true;
            }
        }

        public bool RemoveBinding(string username, IMessageChannel channel)
        {
            using (var db = new TwitterContext(dbOptions))
            {
                var name = username.ToLower();

                var found = db.TwitterChannelBindings.FirstOrDefault(b => b.ChannelId == channel.Id && b.TwitterBinding.TwitterUsername == name);
                if (found == default(TwitterChannelBinding)) return false;

                db.TwitterChannelBindings.Remove(found);
                db.SaveChanges();
                return true;
            }
        }

        public bool RemoveBinding(IEntity<ulong> binding)
        {
            using (var db = new TwitterContext(dbOptions))
            {
                var entities = db.TwitterChannelBindings.Where(b => b.ChannelId == binding.Id);
                if (!entities.Any()) return false;

                db.TwitterChannelBindings.RemoveRange(entities);
                db.SaveChanges();
                return true;
            }
        }

        public IEnumerable<TwitterChannelBinding> GetBindings(IMessageChannel channel)
        {
            using (var db = new TwitterContext(dbOptions))
            return db.TwitterChannelBindings.Where(b => b.ChannelId == channel.Id);
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            dbOptions = map.GetService(typeof(DbContextOptions)) as DbContextOptions ?? throw new ServiceNotFoundException("Database options were not found in service provider.");

            client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

            ModuleColor = new Color(33, 155, 243);
            try
            {
                ModuleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]), byte.Parse(config["color"][2]));
            }
            catch (KeyNotFoundException)
            {
                Logging.LogError(
                        "TwitterService.Init(): The property was not found on the dynamic object. No colors were supplied.")
                    .Wait();
            }
            catch (Exception e)
            {
                Logging.LogError(e.ToString()).Wait();
            }

            twitterSession = new ApplicationSession(config["client_key"], config["client_secret"]);
        }

        async Task ITimedService.Callback()
        {
            using (var db = new TwitterContext(dbOptions))
            {
                foreach (var user in db.TwitterBindings)
                {
                    var response = await twitterSession.GetUserTimeline(user.TwitterUsername);
                    var newestCreationTime = response.FirstOrDefault()?.Time ?? DateTimeOffset.FromUnixTimeSeconds(0);
                    var postStack = new Stack<Embed>();

                    foreach (var tweet in response)
                    {
                        if (tweet.Time <= user.LatestPost) break;
                        user.LatestPost = tweet.Time.DateTime;

                        var embed = new EmbedBuilder()
                            .WithUrl($"https://twitter.com/{tweet.User.ScreenName}/status/{tweet.Id}")
                            .WithDescription(tweet.Text)
                            .WithThumbnailUrl(tweet.User.Avatar)
                            .WithColor(ModuleColor);

                        embed.Title = tweet.IsARetweet() ? $"{tweet.RetweetedStatus.User.Name} (retweeted by @{tweet.User.ScreenName})" : $"{tweet.User.Name} (@{tweet.User.ScreenName})";

                        postStack.Push(embed.Build());
                    }

                    if (newestCreationTime > user.LatestPost) user.LatestPost = newestCreationTime.DateTime;

                    foreach (var embed in postStack)
                    {
                        foreach (var channelBinding in user.ChannelBindings)
                        {
                            if (client.GetChannel(channelBinding.ChannelId) is IMessageChannel channel) await channel.SendEmbedAsync(embed);
                        }
                    }

                    // db.Update(user); // Unnecessary because of tracking queries
                    db.SaveChanges();
                }
            }
        }
    }
}
