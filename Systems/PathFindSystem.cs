using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using XianCraft.Components;
using System.Collections.Generic;
using System.Linq;
using System;

namespace XianCraft.Systems;

public class PathFindSystem : AEntitySetSystem<GameTime>
{
    private readonly World _world;
    private readonly EntitySet _terrainMapSet;

    private Entity _terrainMapEntity => _terrainMapSet.GetEntities().ToArray().FirstOrDefault();


    public PathFindSystem(World world) : base(
        world.GetEntities().With<Position>().With<Movement>().With<MoveCommand>().AsSet())
    {
        _world = world;
        _terrainMapSet = _world.GetEntities().With<TerrainMap>().AsSet();
    }

    protected override void Update(GameTime gameTime, in Entity entity)
    {
        // 这里可以实现路径寻找逻辑
        // 例如，使用A*算法或其他路径寻找算法
        var position = entity.Get<Position>();
        var moveCommand = entity.Get<MoveCommand>();
        var terrainMap = _terrainMapEntity.Get<TerrainMap>();

        var start = new Point((int)position.Value.X, (int)position.Value.Y);
        var end = new Point((int)moveCommand.TargetPosition.X, (int)moveCommand.TargetPosition.Y);
        var path = FindPath(terrainMap, start, end);
        Console.WriteLine($"Path from {start} to {end}: {string.Join(" -> ", path)}");
        entity.Set(new PathData
        {
            Path = path,
        });

        entity.Remove<MoveCommand>();
    }

    private List<Point> FindPath(TerrainMap terrainMap, Point start, Point end)
    {
        var openSet = new PriorityQueue<Point, int>();
        var cameFrom = new Dictionary<Point, Point>();
        var gScore = new Dictionary<Point, int>();
        var fScore = new Dictionary<Point, int>();

        openSet.Enqueue(start, 0);
        gScore[start] = 0;
        fScore[start] = Heuristic(start, end);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == end)
                return ReconstructPath(cameFrom, current);

            foreach (var neighbor in GetNeighbors(terrainMap, current))
            {
                int tentativeGScore = gScore[current] + 1; // 假设每步代价为1
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Heuristic(neighbor, end);
                    if (!openSet.UnorderedItems.Any(x => x.Element == neighbor))
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }
        }
        return new List<Point>(); // 无路径
    }

    private int Heuristic(Point a, Point b)
    {
        // 曼哈顿距离
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private IEnumerable<Point> GetNeighbors(TerrainMap terrainMap, Point point)
    {
        // 4方向移动
        var directions = new[]
        {
            new Point(0, 1),
            new Point(1, 0),
            new Point(0, -1),
            new Point(-1, 0)
        };

        foreach (var dir in directions)
        {
            var neighbor = new Point(point.X + dir.X, point.Y + dir.Y);
            if (terrainMap.IsWalkable(neighbor.X, neighbor.Y))
                yield return neighbor;
        }
    }

    private List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
    {
        var totalPath = new List<Point> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }
        return totalPath;
    }
}
