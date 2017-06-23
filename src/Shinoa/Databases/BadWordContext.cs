﻿// <copyright file="BadWordContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    using Microsoft.EntityFrameworkCore;

    public class BadWordContext : DbContext, IDatabaseContext
    {
        public BadWordContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("badwordfilter")
                .Entity<ChannelBadWord>()
                .HasKey(b => new { b.ChannelIdString, b.Entry });

            modelBuilder
                .Entity<ChannelBadWord>()
                .HasIndex(b => b.ServerIdString);

            modelBuilder
                .Entity<ServerBadWord>()
                .HasKey(b => new { b.ServerIdString, b.Entry });

            modelBuilder
                .Entity<BadWordChannelBinding>()
                .HasIndex(b => b.ServerIdString);

            modelBuilder
                .Entity<BadWordChannelBinding>()
                .HasMany(b => b.BadWords)
                .WithOne();

            modelBuilder
                .Entity<BadWordServerBinding>()
                .HasMany(b => b.BadWords)
                .WithOne();
        }

        public DbSet<BadWordChannelBinding> BadWordChannelBindings { get; set; }

        public DbSet<BadWordServerBinding> BadWordServerBindings { get; set; }

        public class BadWordChannelBinding
        {
            [Key]
            public string ChannelIdString { get; set; }

            [NotMapped]
            public ulong ChannelId
            {
                get => ulong.Parse(ChannelIdString);
                set => ChannelIdString = value.ToString();
            }

            public string ServerIdString { get; set; }

            [NotMapped]
            public ulong ServerId
            {
                get => ulong.Parse(ServerIdString);
                set => ServerIdString = value.ToString();
            }

            public List<ChannelBadWord> BadWords { get; set; }
        }

        public class BadWordServerBinding
        {
            [Key]
            public string ServerIdString { get; set; }

            [NotMapped]
            public ulong ServerId
            {
                get => ulong.Parse(ServerIdString);
                set => ServerIdString = value.ToString();
            }

            public List<ServerBadWord> BadWords { get; set; }
        }

        public class ServerBadWord
        {
            public string ServerIdString { get; set; }

            [NotMapped]
            public ulong ServerId
            {
                get => ulong.Parse(ServerIdString);
                set => ServerIdString = value.ToString();
            }

            public string Entry { get; set; }
        }

        public class ChannelBadWord
        {
            public string ChannelIdString { get; set; }

            [NotMapped]
            public ulong ChannelId
            {
                get => ulong.Parse(ChannelIdString);
                set => ChannelIdString = value.ToString();
            }

            public string ServerIdString { get; set; }

            [NotMapped]
            public ulong ServerId
            {
                get => ulong.Parse(ServerIdString);
                set => ServerIdString = value.ToString();
            }

            public string Entry { get; set; }
        }
    }
}
