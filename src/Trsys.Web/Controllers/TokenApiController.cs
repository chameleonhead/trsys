﻿using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Trsys.Web.Filters;
using Trsys.Web.Models.ReadModel.Events;
using Trsys.Web.Models.ReadModel.Queries;
using Trsys.Web.Models.WriteModel.Commands;

namespace Trsys.Web.Controllers
{
    [Route("api/token")]
    [EaVersion("20210331")]
    [ApiController]
    public class TokenApiController : ControllerBase
    {

        private readonly IMediator mediator;

        public TokenApiController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpPost]
        [Consumes("text/plain")]
        public async Task<IActionResult> PostToken([FromBody] string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return BadRequest("InvalidSecretKey");
            }

            try
            {
                var secretKey = await mediator.Send(new FindBySecretKey(key));
                if (secretKey == null)
                {
                    await mediator.Send(new CreateSecretKeyCommand(null, key, null));
                    await mediator.Publish(new SystemEventNotification("token", "NewEaAccessed", new { SecretKey = key }));
                    return BadRequest("InvalidSecretKey");
                }
                else if (!secretKey.IsApproved)
                {
                    return BadRequest("InvalidSecretKey");
                }
                var token = await mediator.Send(new GenerateSecretTokenCommand(secretKey.Id));
                await mediator.Publish(new SystemEventNotification("token", "TokenGenerated", new { SecretKey = key, SecretToken = token }));
                return Ok(token);
            }
            catch
            {
                return BadRequest("SecretKeyInUse");
            }
        }

        [HttpPost("{token}/release")]
        [Consumes("text/plain")]
        public async Task<IActionResult> PostTokenRelease(string token)
        {
            var secretKey = await mediator.Send(new FindByCurrentToken(token));
            if (secretKey == null)
            {
                return BadRequest("InvalidToken");
            }
            await mediator.Send(new InvalidateSecretTokenCommand(secretKey.Id, token));
            await mediator.Publish(new SystemEventNotification("token", "TokenReleased", new { SecretKey = secretKey, SecretToken = token }));
            return Ok(token);
        }
    }
}
