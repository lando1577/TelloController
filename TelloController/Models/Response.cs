using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelloController.Enums;

namespace TelloController.Models
{
    public class CommandResponse
    {
        public CommandResponse(string actualResponse, ResponseCode code)
        {
            ActualResponse = actualResponse;
            Code = code;
        }

        public string ActualResponse { get; }
        public ResponseCode Code { get; }
    }
}
