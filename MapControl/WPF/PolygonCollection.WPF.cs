﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace MapControl
{
    using Helix.MapCore;

    /// <summary>
    /// An ObservableCollection of IEnumerable of Location. PolygonCollection adds a CollectionChanged
    /// listener to each element that implements INotifyCollectionChanged and, when such an element changes,
    /// fires its own CollectionChanged event with NotifyCollectionChangedAction.Replace for that element.
    /// </summary>
    public class PolygonCollection : ObservableCollection<IEnumerable<Location>>, IWeakEventListener
    {
        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender));

            return true;
        }

        protected override void InsertItem(int index, IEnumerable<Location> polygon)
        {
            if (polygon is INotifyCollectionChanged addedPolygon)
            {
                CollectionChangedEventManager.AddListener(addedPolygon, this);
            }

            base.InsertItem(index, polygon);
        }

        protected override void SetItem(int index, IEnumerable<Location> polygon)
        {
            if (this[index] is INotifyCollectionChanged removedPolygon)
            {
                CollectionChangedEventManager.RemoveListener(removedPolygon, this);
            }

            if (polygon is INotifyCollectionChanged addedPolygon)
            {
                CollectionChangedEventManager.AddListener(addedPolygon, this);
            }

            base.SetItem(index, polygon);
        }

        protected override void RemoveItem(int index)
        {
            if (this[index] is INotifyCollectionChanged removedPolygon)
            {
                CollectionChangedEventManager.RemoveListener(removedPolygon, this);
            }

            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            foreach (var polygon in this.OfType<INotifyCollectionChanged>())
            {
                CollectionChangedEventManager.RemoveListener(polygon, this);
            }

            base.ClearItems();
        }
    }
}
