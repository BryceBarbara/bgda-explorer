﻿/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using WorldExplorer.DataLoaders;
using WorldExplorer.DataModel;

namespace WorldExplorer
{
    public class WorldTreeViewModel : TreeViewItemViewModel
    {
        readonly World _world;

        public WorldTreeViewModel(World world) 
            : base(null, true)
        {
            _world = world;
        }

        public World World()
        {
            return _world;
        }

        public string WorldName
        {
            get { return _world.Name; }
        }

        protected override void LoadChildren()
        {
            _world.Load();

            if (_world.WorldLmp != null)
            {
                base.Children.Add(new LmpTreeViewModel(_world, this, _world.WorldLmp));
            }
            else if (_world.WorldGob != null)
            {
                base.Children.Add(new GobTreeViewModel(_world, this));
            }
            else if (_world.WorldYak != null)
            {
                base.Children.Add(new YakTreeViewModel(this, _world.WorldYak));
            }
            else
            {
                throw new NotSupportedException("Unknown or corrupted file");
            }
        }
    }

    /// <summary>
    /// A simple model that just displays a text label.
    /// </summary>
    public class TextTreeViewModel : TreeViewItemViewModel
    {
        public TextTreeViewModel(World world, TreeViewItemViewModel parent, string text) : base (parent, false)
        {
            _text = text;
        }

        private string _text;

        public string Text
        {
            get { return _text; }
        }
    }

    /// <summary>
    /// A simple model that displays a GOB file.
    /// </summary>
    public class GobTreeViewModel : TreeViewItemViewModel
    {
        public GobTreeViewModel(World world, TreeViewItemViewModel parent)
            : base(parent, true)
        {
            _world = world;
        }

        private World _world;

        public string Text
        {
            get { return _world.WorldGob.Filename; }
        }

        protected override void LoadChildren()
        {
            foreach (var entry in _world.WorldGob.Directory)
            {
                Children.Add(new LmpTreeViewModel(_world, this, entry.Value));
            }
        }
    }

    /// <summary>
    /// A simple model that displays a YAK file.
    /// </summary>
    public class YakTreeViewModel : TreeViewItemViewModel
    {
        public YakTreeViewModel(TreeViewItemViewModel parent, YakFile yakFile) : base(parent, true)
        {
            _parent = parent;
            _yakFile = yakFile;
            _name = yakFile.Name;
        }

        private TreeViewItemViewModel _parent;
        private YakFile _yakFile;
        private String _name;

        public string Text
        {
            get { return _name; }
        }

        protected override void LoadChildren()
        {
            _yakFile.ReadEntries();
            var entries = _yakFile.Entries;
            int i = 0;
            foreach (var entry in entries)
            {
                Children.Add(new YakTreeViewItem(this, _yakFile, entry, "Entry "+i));
                ++i;
            }
        }
    }

    public class YakTreeViewItem : TreeViewItemViewModel
    {
        public YakTreeViewItem(TreeViewItemViewModel parent, YakFile yakFile, YakFile.Entry entry, String name)
            : base(parent, true)
        {
            _yakFile = yakFile;
            _entry = entry;
            _name = name;
        }

        private YakFile _yakFile;
        private YakFile.Entry _entry;
        private String _name;

        public string Text
        {
            get { return _name; }
        }

        protected override void LoadChildren()
        {
            int i = 0;
            foreach (var child in _entry.children)
            {
                Children.Add(new YakChildTreeViewItem(this, _yakFile, child, _entry, "Child " + i));
                ++i;
            }
        }
    }

    public class YakChildTreeViewItem : TreeViewItemViewModel
    {
        public YakChildTreeViewItem(TreeViewItemViewModel parent, YakFile yakFile, YakFile.Child value, YakFile.Entry entry, String name)
            : base(parent, false)
        {
            _yakFile = yakFile;
            _value = value;
            _entry = entry;
            _name = name;
        }

        private YakFile _yakFile;
        private YakFile.Child _value;
        private YakFile.Entry _entry;
        private String _name;

        public YakFile.Child Value
        {
            get { return _value; }
        }

        public YakFile.Entry ParentEntry
        {
            get { return _entry; }
        }

        public YakFile YakFile
        {
            get { return _yakFile; }
        }

        public string Text
        {
            get { return _name; }
        }
    }

    public abstract class AbstractLmpTreeViewModel : TreeViewItemViewModel
    {
        public AbstractLmpTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
            : base(parent, true)
        {
            _lmpFile = lmpFile;
            _name = entryName;
            _world = world;
        }

        protected LmpFile _lmpFile;
        protected string _name;
        protected World _world;

        public LmpFile LmpFileProperty
        {
            get { return _lmpFile; }
        }

        public string Text
        {
            get { return _name; }
        }
    }

    /// <summary>
    /// A simple model that displays a LMP file.
    /// </summary>
    public class LmpTreeViewModel : AbstractLmpTreeViewModel
    {
        public LmpTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile)
            : base(world, parent, lmpFile, lmpFile.Name)
        {
        }

        protected override void LoadChildren()
        {
            _lmpFile.ReadDirectory();
            foreach (var entry in _lmpFile.Directory)
            {
                var ext = (System.IO.Path.GetExtension(entry.Key) ?? "").ToLower();

                TreeViewItemViewModel child;
                switch (ext)
                {
                    case ".world":
                        child = new WorldFileTreeViewModel(_world, this, _lmpFile, entry.Key);
                        break;
                    default:
                        child = new LmpEntryTreeViewModel(_world, this, _lmpFile, entry.Key);
                        break;
                }
                Children.Add(child);
            }
        }
    }

    /// <summary>
    /// A simple model that displays an entry in a LMP file.
    /// </summary>
    public class LmpEntryTreeViewModel : AbstractLmpTreeViewModel
    {
        public LmpEntryTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
            : base(world, parent, lmpFile, entryName)
        {
        }

        protected override void LoadChildren()
        {
            
        }
    }

    public class WorldFileTreeViewModel : AbstractLmpTreeViewModel
    {
        public WorldFileTreeViewModel(World world, TreeViewItemViewModel parent, LmpFile lmpFile, string entryName)
            : base(world, parent, lmpFile, entryName)
        {
        }

        public void ReloadChildren()
        {
            Children.Clear();
            LoadChildren();
        }

        protected override void LoadChildren()
        {
            if (_world.worldData == null)
            {
                // Force loading the tree item
                this.IsSelected = true;
                return; // Return to prevent adding elements twice
            }
            if (_world.worldData != null){
                int i = 0;
                foreach (var element in _world.worldData.worldElements)
                {
                    Children.Add(new WorldElementTreeViewModel(element, Parent, "Element " + i));
                    ++i;
                }
            }
        }
    }

    public class WorldElementTreeViewModel : TreeViewItemViewModel
    {
        public WorldElementTreeViewModel(WorldElement worldElement, TreeViewItemViewModel parent, string name)
            : base(parent, true)
        {
            _worldElement = worldElement;
            _name = name;
        }

        private string _name;
        private WorldElement _worldElement;

        public WorldElement WorldElement
        { get { return _worldElement; } }

        public string Text
        {
            get { return _name; }
        }

        protected override void LoadChildren()
        {
            
        }
    }
}
