﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rooms.CardinalDirections;
using UnityEngine;
using Utils;
using static Rooms.RoomType;
using Random = UnityEngine.Random;

namespace Rooms.NeighborsStrategy
{
    public class MazeStrategy : INeighborsStrategy
    {
        #region Fields

        private readonly int _maxDistanceToBoss;
        private readonly int _minDistanceToBoss;
        private readonly int _totalRooms; // number of rooms will be (maxDistance * roomPopulation)

        private Vector2Int _bossIndex;

        private readonly Dictionary<Vector2Int, (RoomType type, int bossDistance)> _rooms;

        private readonly int _fountainDistance;
        private readonly int _treasureDistance;
        private int _fountainCount;
        private int _treasureCount;

        private HashSet<Vector2Int> _specialLocations;

        #endregion

        #region Construcors

        public MazeStrategy(int minDistanceToBoss, int maxDistanceToBoss, int totalRooms, int numFountains = 0,
            int numTreasure = 0)
        {
            _minDistanceToBoss = minDistanceToBoss;
            _maxDistanceToBoss = maxDistanceToBoss;

            _totalRooms = (int)Math.Min(Math.Max(totalRooms, maxDistanceToBoss),
                Mathf.Pow(2 * maxDistanceToBoss + 1, 2));

            _fountainDistance =
                (int)Mathf.Ceil(((float)_totalRooms / ((numFountains + 1) * _totalRooms)) * _maxDistanceToBoss);
            _treasureDistance =
                (int)Mathf.Ceil(((float)_totalRooms / ((numTreasure + 1) * _totalRooms)) * _maxDistanceToBoss);

            _rooms = new();
            _specialLocations = new();
            CreateMaze();
        }

        #endregion

        #region INeighborsStrategy Implementation

        public float RoomIntensity(Vector2Int index) => 1 - _rooms[index].bossDistance / (2f * _maxDistanceToBoss);

        public RoomType RoomType(Vector2Int index) =>
            _rooms.ContainsKey(index) ? _rooms[index].type : NONE;

        public int RoomRank(int minRoomRank, Vector2Int index, AnimationCurve distanceToRankFunction)
        {
            return index == _bossIndex
                ? 0
                : minRoomRank +
                  (int)(distanceToRankFunction.Evaluate(index.L1Norm() / (float)_maxDistanceToBoss) * minRoomRank);
        }

        #endregion

        #region Private Methods

        private void PrintMaze(bool distance = false)
        {
            StringBuilder s = new();
            for (int row = _maxDistanceToBoss; row >= -_maxDistanceToBoss; row--)
            {
                for (int col = -_maxDistanceToBoss; col <= _maxDistanceToBoss; col++)
                {
                    var room = new Vector2Int(col, row);
                    if (!_rooms.ContainsKey(room))
                        s.Append(" - ");
                    else
                    {
                        if (distance)
                        {
                            s.Append($" {_rooms[room].bossDistance} ");
                            continue;
                        }

                        s.Append(_rooms[room].type switch
                        {
                            START => " S ",
                            BOSS => " B ",
                            TREASURE => " T ",
                            FOUNTAIN => " F ",
                            MONSTERS => " M ",
                            _ => " - "
                        });
                    }
                }

                s.Append("\n");
            }

            Debug.Log(s);
        }

        private void CreateMaze()
        {
            AddRoom(Vector2Int.zero);

            CreatePathToBoss();
            PopulateMaze();
            MarkRoomBossDistance();

            PrintMaze();
            PrintMaze(true);
        }

        private void MarkRoomBossDistance()
        {
            Queue<Vector2Int> currDistanceQueue = new Queue<Vector2Int>();
            Queue<Vector2Int> nextDistanceQueue = new Queue<Vector2Int>();

            _rooms[_bossIndex] = (BOSS, 0);
            nextDistanceQueue.Enqueue(_bossIndex + Direction.SOUTH.ToVector());

            //BFS until we populate enough rooms
            for (int currDistance = 1; nextDistanceQueue.Count > 0; ++currDistance)
            {
                // move next to curr. curr is empty so it can be used ass the new next queue
                (currDistanceQueue, nextDistanceQueue) = (nextDistanceQueue, currDistanceQueue);

                while (currDistanceQueue.Count > 0)
                {
                    var roomIndex = currDistanceQueue.Dequeue();
                    var roomEntry = _rooms[roomIndex];
                    if (roomEntry.bossDistance >= 0) // already marked
                        continue;
                    roomEntry.bossDistance = currDistance;
                    _rooms[roomIndex] = roomEntry;
                    foreach (Direction dir in DirectionExt.GetDirections())
                    {
                        var neighborIndex = roomIndex + dir.ToVector();
                        if (_rooms.ContainsKey(neighborIndex))
                            nextDistanceQueue.Enqueue(neighborIndex);
                    }
                }
            }
        }

        //randomly add rooms to Set (using BFS to make sure they're all reachable)
        private void PopulateMaze()
        {
            Queue<Vector2Int> roomsToAdd = new Queue<Vector2Int>();
            HashSet<Vector2Int> queued = new HashSet<Vector2Int>();

            roomsToAdd.Enqueue(Vector2Int.zero);
            queued.Add(Vector2Int.zero);

            //BFS until we populate enough rooms
            while (_rooms.Count < _totalRooms)
            {
                if (roomsToAdd.Count == 0)
                {
                    roomsToAdd.Enqueue(Vector2Int.zero);
                    queued.Clear();
                    queued.Add(Vector2Int.zero);
                }

                var currRoom = roomsToAdd.Dequeue();
                AddRoom(currRoom);

                foreach (Direction dir in DirectionExt.GetDirections())
                {
                    var newRoom = currRoom + dir.ToVector();
                    if (queued.Contains(newRoom) ||
                        Vector2Ext.L1Distance(Vector2Int.zero, newRoom) > _maxDistanceToBoss) continue;

                    /*
                     * might prefer to try 'if (Random.value > 0.5f || _rooms.Contains(newRoom))' for faster generation 
                     */
                    if (Random.value > 0.5f)
                    {
                        roomsToAdd.Enqueue(newRoom);
                        queued.Add(newRoom);
                    }
                }
            }
        }

        private void CreatePathToBoss()
        {
            /*
             * minDistance < row+col < maxDistance -> minDistance-row < col < maxDistance-row
             */
            var row = Random.Range(0, _maxDistanceToBoss);
            var col = Random.Range(_minDistanceToBoss - row, _maxDistanceToBoss - row);

            row *= (Random.value > 0.5) ? 1 : -1;
            col *= (Random.value > 0.5) ? 1 : -1;

            _bossIndex = new Vector2Int(col, row);
            AddRoom(_bossIndex);

            Vector2Int currRoom = _bossIndex + Vector2Int.down;
            AddRoom(currRoom);
            int roomCount = 1;
            Vector2Int lastDir = Vector2Int.zero;

            while (currRoom != Vector2Int.zero)
            {
                int roomsLeft = _maxDistanceToBoss - roomCount;
                int distance = Vector2Ext.L1Norm(currRoom);

                if (distance > roomsLeft + 1)
                {
                    PrintMaze();
                    Debug.Log($"Distance: {distance}, RoomsLeft: {roomsLeft}");
                    throw new Exception(
                        "Got too far from start. this shouldn't happen, something is wrong with the algorithm");
                }

                //if needed create shortest path
                if (distance >= roomsLeft)
                {
                    CreateStraightPath(currRoom, Vector2Int.zero);
                    break;
                }

                // --------------- create random path ----------------------------
                //Get all possible directions
                var possible = new List<Vector2Int>();
                foreach (Direction dir in DirectionExt.GetDirections())
                {
                    Vector2Int newCurr = currRoom + dir.ToVector();
                    if (_rooms.ContainsKey(newCurr) ||
                        Vector2Ext.L1Distance(newCurr, Vector2Int.zero) > roomsLeft - 1) continue;

                    possible.Add(dir.ToVector());
                }

                //If no direction is possible, go back one room
                if (possible.Count == 0)
                {
                    currRoom -= lastDir;
                    roomCount--;
                }
                //else, pick random direction and move in it
                else
                {
                    Vector2Int moveDir = possible[Random.Range(0, possible.Count)];
                    currRoom += moveDir;

                    AddRoom(currRoom);
                    roomCount++;
                }
            }
        }

        /**
         * Adds to _rooms a shortest path from 'from' to 'to'. i.e., for path p it holds |p|=Distance(from,to).
         */
        private void CreateStraightPath(Vector2Int from, Vector2Int to)
        {
            Vector2Int curr = from;
            AddRoom(curr);

            Vector2Int yDir = Math.Sign(to.y - from.y) * Vector2Int.up;
            Vector2Int xDir = Math.Sign(to.x - from.x) * Vector2Int.right;

            while (curr != to)
            {
                var dir = Vector2Int.zero;
                if (curr.x == to.x)
                    dir = yDir;
                else if (curr.y == to.y)
                    dir = xDir;
                else
                    dir = (Random.value > 0.5f) ? yDir : xDir;

                curr += dir;
                AddRoom(curr);
            }
        }

        private void AddRoom(Vector2Int index)
        {
            if (_rooms.ContainsKey(index))
                return;
            _rooms[index] = (GenerateType(index), -1);
        }

        private RoomType GenerateType(Vector2Int index)
        {
            if (index == Vector2Int.zero)
                return START;
            if (index == _bossIndex)
                return BOSS;

            var dist = index.L1Norm(); // equivalent to L1Distance(zero, index)
            if (_fountainDistance > 0
                && _specialLocations.All(location => Vector2Ext.L1Distance(location, index) > _fountainDistance))
            {
                var poss = _rooms.Count / (_fountainDistance * (_fountainCount + 1) + _fountainDistance);
                if (dist / _fountainDistance > _fountainCount && Random.value < poss)
                {
                    _fountainCount++;
                    _specialLocations.Add(index);
                    return FOUNTAIN;
                }
            }

            if (_treasureDistance > 0
                && _specialLocations.All(location => Vector2Ext.L1Distance(location, index) >= _treasureDistance))
            {
                var poss = _rooms.Count / (_treasureDistance * (_treasureCount + 1) + _treasureDistance);
                if (dist / _treasureDistance > _treasureCount && Random.value < poss)
                {
                    _treasureCount++;
                    _specialLocations.Add(index);
                    return TREASURE;
                }
            }

            return MONSTERS;
        }

        #endregion
    }
}