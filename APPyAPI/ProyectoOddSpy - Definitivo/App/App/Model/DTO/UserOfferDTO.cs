using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Model.DTO
{
    public class UserOfferDTO
    {
        public Guid Id { get; set; }
        public Offers Offer { get; set; }
    }
}
