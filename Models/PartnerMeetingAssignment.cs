using System.ComponentModel.DataAnnotations;

namespace SoftflipSolutions.Models;

public class PartnerMeetingAssignment
{
    [Key]
    public int Id { get; set; }

    public int PartnerMeetingId { get; set; }
    public PartnerMeeting? PartnerMeeting { get; set; }

    public int ChannelPartnerId { get; set; }
    public ChannelPartner? ChannelPartner { get; set; }
}
