﻿using System;
using System.Collections.Generic;
using System.Threading;
using Planetarium.Objects;
using Planetarium.Types;

namespace Planetarium.Types
{
    public interface ISky
    {
        SkyContext Context { get; }

        event Action Calculated;

        void Calculate();
        List<List<Ephemeris>> GetEphemerides(CelestialObject body, double from, double to, double step, IEnumerable<string> categories, CancellationToken? cancelToken = null, IProgress<double> progress = null);
        ICollection<string> GetEphemerisCategories(CelestialObject body);
        ICollection<AstroEvent> GetEvents(double jdFrom, double jdTo, IEnumerable<string> categories, CancellationToken? cancelToken = null);
        ICollection<string> GetEventsCategories();
        CelestialObjectInfo GetInfo(CelestialObject body);
        string GetObjectName(CelestialObject body);
        void Initialize();
        ICollection<SearchResultItem> Search(string searchString, Func<CelestialObject, bool> filter);
    }
}