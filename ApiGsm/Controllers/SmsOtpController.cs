using ApiGsm.Utils;

namespace ApiGsm.Controllers
{
    [Route("api/v1/otp")]
    [ApiController]
    public class SmsOtpController : ControllerBase
    {
        [Route("all")]
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(DatabaseOtp.GetAllOtp());
        }

        [Route("send/{numberPhone}")]
        [HttpPost]
        public IActionResult Post(string numberPhone)
        {
            return Ok(DatabaseOtp.AddOtp(numberPhone));
        }
    }
}