using System;
using System.Collections.Generic;
using Shiny.Spatial.Algorithms;
using Shiny.Spatial.Geometry;

namespace Shiny.Spatial.Database;

public sealed class SpatialQuery
{
    private readonly SpatialTable _table;
    private Envelope? _bbox;
    private readonly List<PropertyFilter> _propertyFilters = new();
    private Func<SpatialFeature, bool>? _geometryFilter;
    private Coordinate? _distanceCenter;
    private double? _distanceMeters;
    private Coordinate? _orderByDistanceFrom;
    private int? _limit;
    private int _offset;

    internal SpatialQuery(SpatialTable table)
    {
        _table = table;
    }

    public SpatialQuery InEnvelope(Envelope envelope)
    {
        _bbox = _bbox == null ? envelope : _bbox.Value.ExpandToInclude(envelope);
        return this;
    }

    public SpatialQuery Intersecting(Geometry.Geometry geometry)
    {
        var env = geometry.GetEnvelope();
        InEnvelope(env);
        _geometryFilter = feature => SpatialPredicates.Intersects(feature.Geometry, geometry);
        return this;
    }

    public SpatialQuery ContainedBy(Geometry.Geometry container)
    {
        var env = container.GetEnvelope();
        InEnvelope(env);
        _geometryFilter = feature => SpatialPredicates.Contains(container, feature.Geometry);
        return this;
    }

    public SpatialQuery WithinDistance(Coordinate center, double distanceMeters)
    {
        _distanceCenter = center;
        _distanceMeters = distanceMeters;

        var pointEnv = new Envelope(center.X, center.X, center.Y, center.Y);
        var searchEnv = EnvelopeExpander.ExpandByDistance(pointEnv, distanceMeters, _table.CoordinateSystem);
        InEnvelope(searchEnv);

        _geometryFilter = feature =>
        {
            var featureCenter = feature.Geometry.GetEnvelope().Center;
            double dist = _table.CoordinateSystem == CoordinateSystem.Wgs84
                ? DistanceCalculator.Haversine(center, featureCenter)
                : DistanceCalculator.Euclidean(center, featureCenter);
            return dist <= distanceMeters;
        };

        return this;
    }

    public SpatialQuery WhereProperty(string propertyName, string op, object value)
    {
        _propertyFilters.Add(new PropertyFilter(propertyName, op, value));
        return this;
    }

    public SpatialQuery OrderByDistance(Coordinate center)
    {
        _orderByDistanceFrom = center;
        return this;
    }

    public SpatialQuery Limit(int count)
    {
        _limit = count;
        return this;
    }

    public SpatialQuery Offset(int count)
    {
        _offset = count;
        return this;
    }

    public List<SpatialFeature> ToList()
    {
        return _table.ExecuteQuery(
            _bbox,
            _propertyFilters.Count > 0 ? _propertyFilters : null,
            _geometryFilter,
            _orderByDistanceFrom,
            _limit,
            _offset
        );
    }

    public int Count()
    {
        var results = ToList();
        return results.Count;
    }

    public SpatialFeature? FirstOrDefault()
    {
        var oldLimit = _limit;
        _limit = 1;
        var results = ToList();
        _limit = oldLimit;
        return results.Count > 0 ? results[0] : null;
    }
}
