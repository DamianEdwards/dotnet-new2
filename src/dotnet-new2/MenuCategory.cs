using System.Collections.Generic;

namespace dotnet_new2
{
    public class MenuCategory : MenuNode
    {
        public IList<MenuNode> Children { get; set; }
    }
}
