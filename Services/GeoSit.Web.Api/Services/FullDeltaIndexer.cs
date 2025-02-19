﻿using GeoSit.Web.Api.Services.Interfaces;
using System;
using System.Configuration;
using System.Threading;

namespace GeoSit.Web.Api.Services
{
    internal class FullDeltaIndexer : IStandaloneService
    {
        private readonly int MINUTES;
        private readonly Timer _indexedTimer;
        private readonly Mutex _mtxProcessing;
        public FullDeltaIndexer()
        {
            MINUTES = int.Parse(ConfigurationManager.AppSettings["CADENCIA_MINUTOS_ACTUALIZACION_SOLR"]);
            _indexedTimer = new Timer(callIndexed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _mtxProcessing = new Mutex();
        }
        public void Start()
        {
            if (MINUTES > 0)
            {
                _indexedTimer.Change(TimeSpan.FromMinutes(.5), Timeout.InfiniteTimeSpan);
            }
        }

        public void Stop()
        {
            while (!_mtxProcessing.WaitOne(100))
            {
                Thread.Sleep(100);
            }
            _indexedTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        private void callIndexed(object state)
        {
            try
            {
                _mtxProcessing.WaitOne();
                Solr.SolrUpdater.Instance.Enqueue(Solr.Entities.all);
            }
            finally
            {
                _indexedTimer.Change(TimeSpan.FromMinutes(MINUTES), Timeout.InfiniteTimeSpan);
                _mtxProcessing.ReleaseMutex();
            }
        }
    }
}