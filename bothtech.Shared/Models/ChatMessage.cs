using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bothtech.Shared.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public string FirebaseId { get; set; }
    public string UserUid { get; set; }
    public string Sender { get; set; }
    public string Status { get; set; }
    public string Text { get; set; }
    public string AudioBase64 { get; set; }
    public long Timestamp { get; set; }
}
