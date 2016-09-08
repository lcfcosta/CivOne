// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using CivOne.Enums;
using CivOne.Interfaces;
using CivOne.Screens;

namespace CivOne
{
	internal class Player
	{
		private readonly ICivilization _civilization;
		private readonly string _leaderName, _tribeName, _tribeNamePlural;

		private readonly bool[,] _explored = new bool[Map.WIDTH, Map.HEIGHT];
		private readonly bool[,] _visible = new bool[Map.WIDTH, Map.HEIGHT];
		private readonly List<byte> _advances = new List<byte>();
		
		private short _gold;
		private short _science;
		private IAdvance _currentResearch = null;
		
		private Map Map
		{
			get
			{
				return Map.Instance;
			}
		}

		public ICivilization Civilization
		{
			get
			{
				return _civilization;
			}
		}
		
		public string LeaderName
		{
			get
			{
				return _leaderName;
			}
		}
		
		public string TribeName
		{
			get
			{
				return _tribeName;
			}
		}
		
		public string TribeNamePlural
		{
			get
			{
				return _tribeNamePlural;
			}
		}

		public bool Human
		{
			get
			{
				return (Game.Instance.HumanPlayer == this);
			}
		}

		public City[] Cities
		{
			get
			{
				return Game.Instance.GetCities().Where(c => c.Owner == _civilization.PreferredPlayerNumber).ToArray();
			}
		}

		public int Population
		{
			get
			{
				return Cities.Sum(c => c.Population);
			}
		}
		
		public short Gold
		{
			get
			{
				return _gold;
			}
			internal set
			{
				_gold = value;
			}
		}
		
		public short Science
		{
			get
			{
				return _science;
			}
			internal set
			{
				// Temporary code until the science code is implemented
				_science = value;
				if (_science == 8)
				{
					_advances.Add(_currentResearch.Id);
					if (Human)
					{
						Screens.Civilopedia civilopedia = new Screens.Civilopedia(_currentResearch);
						civilopedia.Closed += (s, a) => {
							_currentResearch = null;
							Science = 0;
						};
						Common.AddScreen(civilopedia);
					}
				}
				if (_currentResearch == null)
				{
					if (Human)
						Common.AddScreen(new ChooseTech());
				}
			}
		}
		
		public string LatestAdvance
		{
			get
			{
				if (_advances.Count == 0)
					return "Irrigation";
				return Reflect.GetAdvances().First(a => a.Id == _advances.Last()).Name;
			}
		}

		public IAdvance[] Advances
		{
			get
			{
				return _advances.Select(a => Reflect.GetAdvances().First(x => x.Id == a)).ToArray();
			}
		}

		public IAdvance CurrentResearch
		{
			get
			{
				return _currentResearch;
			}
			set
			{
				_currentResearch = value;
			}
		}

		public IEnumerable<IAdvance> AvailableResearch
		{
			get
			{
				foreach (IAdvance advance in Reflect.GetAdvances().Where(a => !_advances.Contains(a.Id)))
				{
					if (advance.RequiredTechs.Length > 0 && !advance.RequiredTechs.All(a => _advances.Contains(a.Id))) continue;
					yield return advance;
				}
			}
		}

		private bool UnitAvailable(IUnit unit)
		{
			// Determine if the unit is obsolete
			if (_advances.Any(a => unit.ObsoleteTech != null && unit.ObsoleteTech.Id == a))
				return false;
			
			// Determine if the unit requires a tech
			if (unit.RequiredTech == null)
				return true;
			
			// Determine if the Player has the required tech
			if (_advances.Any(a => unit.RequiredTech.Id == a))
				return true;
			
			return false;
		}

		public bool ProductionAvailable(IProduction production)
		{
			if (production is IUnit)
				return UnitAvailable(production as IUnit);
			return true;
		}

		public void Explore(int x, int y, int range = 1, bool sea = false)
		{
			_explored[x, y] = true;
			for (int relX = -range; relX <= range; relX++)
			for (int relY = -range; relY <= range; relY++)
			{
				int xx = x + relX;
				int yy = y + relY;
				if (yy < 0 || yy >= Map.HEIGHT) continue;
				while (xx < 0) xx += Map.WIDTH;
				while (xx >= Map.WIDTH) xx -= Map.WIDTH;
				if (sea && !Map[xx, yy].IsOcean && (Math.Abs(relX) > 1 || Math.Abs(relY) > 1))
					continue;
				_visible[xx, yy] = true;
			} 
		}

		public bool Visible(int x, int y)
		{
			if (y < 0 || y >= Map.HEIGHT) return false;
			while (x < 0) x += Map.WIDTH;
			while (x >= Map.WIDTH) x -= Map.WIDTH;
			return _visible[x, y];
		}

		public bool Visible(ITile tile)
		{
			if (tile == null) return false;
			return Visible(tile.X, tile.Y);
		}

		public bool Visible(ITile tile, Direction direction)
		{
			if (tile == null) return false;
			return Visible(tile.GetBorderTile(direction));
		}
		
		public Player(ICivilization civilization, string customLeaderName = null, string customTribeName = null, string customTribeNamePlural = null)
		{
			_civilization = civilization;
			_leaderName = customLeaderName ?? _civilization.LeaderName;
			_tribeName = customTribeName ?? _civilization.Name;
			_tribeNamePlural = customTribeNamePlural ?? _civilization.NamePlural;
			
			for (int xx = 0; xx < Map.WIDTH; xx++)
			for (int yy = 0; yy < Map.HEIGHT; yy++)
			{
				_explored[xx, yy] = false;
				_visible[xx, yy] = false;
			}
		}
	}
}