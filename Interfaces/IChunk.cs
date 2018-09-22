namespace Archiver.Interfaces
{
    public interface IChunk : IIndexedItem
    {
        byte[] Body { get; set; }
    }
}
