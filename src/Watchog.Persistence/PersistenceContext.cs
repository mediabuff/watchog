﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watchog.IPC;

namespace Watchog.Persistence
{
    public sealed class PersistenceContext : IDisposable
    {
        private IPCPeer _peer;
        private SharedMemoryListener _sharedMemoryListener;
        private WatchogDB _db;

        private PersistenceContext()
        {}

        public async static Task<PersistenceContext> Create(string dbPath)
        {
            var context = new PersistenceContext
            {
                _peer = new IPCPeer(),
                _db = new WatchogDB(dbPath)
            };

            var movies = await context._db.GetAllAsWrappers();
            context._peer.Reset(new MovieListWrapper
            {
                Movies = movies
            });

            foreach (var movie in movies)
            {
                ShellUtils.RefreshShellIcon(movie.Path);
            }

            context._sharedMemoryListener = new SharedMemoryListener(context._peer);
            context._sharedMemoryListener.SharedMemoryChanged += context.OnSharedMemoryChanged;
            context._sharedMemoryListener.Start();

            return context;
        }

        private void OnSharedMemoryChanged(List<MovieWrapper> movies)
        {
            _db.ApplyChanges(movies).Wait();
        }

        public void Dispose()
        {
            _sharedMemoryListener.Dispose();
            _peer.Dispose();
        }
    }
}
