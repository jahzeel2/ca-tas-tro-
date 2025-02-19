namespace SGTEntities.Interfaces
{
    public interface ITextGenerator<T>
    {
        string Generate(T param);
    }
}
