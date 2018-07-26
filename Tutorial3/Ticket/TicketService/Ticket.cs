namespace TicketService
{
    public class Ticket
    {
        public int Id { get; private set; }
        public string BarCode { get; private set; }

        static public Ticket FindBy(int id)
        {
            return new Ticket() { Id = id, BarCode = System.Guid.NewGuid().ToString() };
        }
    }
}