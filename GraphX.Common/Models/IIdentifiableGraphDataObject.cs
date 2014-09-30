using System;

namespace GraphX
{
    public interface IIdentifiableGraphDataObject
    {
        Guid ID { get; set; }
    }
}
