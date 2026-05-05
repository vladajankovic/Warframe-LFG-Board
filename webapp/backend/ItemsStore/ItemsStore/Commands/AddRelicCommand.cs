using ItemsStore.Database.Entity;

namespace ItemsStore.Commands
{
    public class AddRelicCommand
    {
        public string Name { get; set; }
        public RelicType Type { get; set; }
        public string Code { get; set; }
    }
}
