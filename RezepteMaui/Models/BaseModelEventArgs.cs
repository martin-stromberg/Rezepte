namespace Rezepte.Models
{
    public class BaseModelEventArgs: EventArgs
    {
        public BaseModelEventArgs(BaseModel item)
        {
            Item = item;
        }

        public BaseModel Item { get; }
    }
}
