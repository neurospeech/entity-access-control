namespace NeuroSpeech.EntityAccessControl
{
    public interface IQueryContext
    {
        IQueryContext<T> OfType<T>() where T: class;
    } 
}
