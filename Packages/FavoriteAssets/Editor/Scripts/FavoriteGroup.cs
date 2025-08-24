using System;
using UnityEngine;

namespace FavoriteAssets.Editor
{
    [Serializable]
    public class FavoriteGroup
    {
        [SerializeField] private string _id;
        [SerializeField] private string _name;
        [SerializeField] private bool _isCollapsed;
        [SerializeField] private long _dateCreatedTicks;
        [SerializeField] private int _sortOrder;
        
        public string Id => _id;
        public string Name => _name;
        public bool IsCollapsed => _isCollapsed;
        public DateTime DateCreated => _dateCreatedTicks == 0 ? DateTime.Now : new DateTime(_dateCreatedTicks);
        public int SortOrder => _sortOrder;
        
        public FavoriteGroup(string name, int sortOrder = 0)
        {
            _id = Guid.NewGuid().ToString();
            _name = name;
            _isCollapsed = false;
            _dateCreatedTicks = DateTime.Now.Ticks;
            _sortOrder = sortOrder;
        }
        
        public void SetName(string name)
        {
            _name = name;
        }
        
        public void SetCollapsed(bool collapsed)
        {
            _isCollapsed = collapsed;
        }
        
        public void SetSortOrder(int order)
        {
            _sortOrder = order;
        }
    }
}