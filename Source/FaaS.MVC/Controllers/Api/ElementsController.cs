﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FaaS.MVC.Models;
using FaaS.Services;
using FaaS.DataTransferModels;
using FaaS.Services.RandomId;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace FaaS.MVC.Controllers.Api
{
    [Route(RoutePrefix + RouteController)]
    public class ElementsController : DefaultController
    {
        public const string RouteController = "elements";

        /// <summary>
        /// Form service
        /// </summary>
        private readonly IFormService formService;

        private readonly IUserService userService;

        /// <summary>
        /// Element service
        /// </summary>
        private readonly IElementService elementService;

        private readonly ILogger<ElementsController> logger;

        public ElementsController(IRandomIdService randomId,
            IActionContextAccessor actionContextAccessor,
            IHttpContextAccessor httpContextAccessor,
            IUrlHelperFactory urlHelperFactory,
            IMapper mapper,
            IUserService userService,
            IFormService formService,
            IElementService elementService,
            ILogger<ElementsController> logger)
            : base(randomId, actionContextAccessor, httpContextAccessor, urlHelperFactory, mapper)
        {
            this.userService = userService;
            this.formService = formService;
            this.elementService = elementService;
            this.logger = logger;
        }

        // GET elements
        [HttpGet]
        public async Task<IActionResult> GetAllElements(
            [FromQuery(Name = "formId")] Guid formId,
            [FromQuery(Name = "limit")]int limit,
            [FromQuery(Name = "attributes")]string[] attributes)
        {
            var formDto = await formService.Get(formId);
            if (formDto == null)
            {
                return NotFound("Form not found with guid:" + formId);
            }

            var elements = await elementService.GetAllForForm(formDto);

            // Apply limit
            if (limit > 0)
            {
                elements = elements.Take(limit).ToArray();
            }

            // Select only given fields
            if (attributes != null && attributes.Any())
            {
                elements = elements.Select(user =>
                {
                    var projection = new Element();

                    foreach (var attribute in attributes)
                    {
                        projection.GetType()
                            .GetProperty(attribute)
                            .SetValue(projection, user.GetType().GetProperty(attribute).GetValue(user));
                    }

                    return projection;
                }).ToArray();
            }

            logger.LogInformation($"Retrieved {elements.Length} elements.");
            return Ok(elements);
        }

        // GET element/{id}/
        [HttpGet("{id}")]
        public async Task<IActionResult> GetElement(Guid id)
        {
            var element = await elementService.Get(id);
            if (element == null)
            {
                return NotFound("Cannot find element with guid: " + id);
            }

            return Ok(element);
        }

        // POST elements
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ElementViewModel element)
        {
            try
            {
                var elementDto = mapper.Map<ElementViewModel, Element>(element);
                var formId = element.FormId;
                var formDto = await formService.Get(formId);
                var result = await elementService.Add(formDto, elementDto);

                var urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
                var newUrl = new Uri(urlHelper.Action("GetElement", "Elements", new
                {
                    id = elementDto.Id,
                }, httpContextAccessor.HttpContext.Request.Scheme));
                logger.LogInformation("[CREATE] element: {} ", elementDto);


                return Created(newUrl, result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT 
        [HttpPatch]
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] ElementViewModel element, [FromQuery(Name = "userId")] string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    Response.StatusCode = 401;
                    return Unauthorized();
                }

                var elementDto = mapper.Map<ElementViewModel, Element>(element);

                // Access validation
                var elementOwner = await userService.Get(new Guid(userId));
                var elementToBeUpdated = await elementService.Get(elementDto.Id);
                if (elementToBeUpdated.Form.Project.User.Email == elementOwner.Email)
                {
                    var result = await elementService.Update(elementDto);
                    logger.LogInformation("[UPDATE] element: {} ", elementDto);
                    return Ok(result);
                }
                else
                {
                    Response.StatusCode = 401;
                    return BadRequest("You tried to update an element that does not belong to you");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE elements/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery(Name = "userId")] string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    Response.StatusCode = 401;
                    return Unauthorized();
                }

                // Access validation
                var elementOwner = await userService.Get(new Guid(userId));
                var elementToBeDeleted = await elementService.Get(id);
                if (elementToBeDeleted.Form.Project.User.Email == elementOwner.Email)
                {
                    var result = await elementService.Remove(elementToBeDeleted);
                    logger.LogInformation("[DELETE] element: {} ", elementToBeDeleted);
                    return Ok(result);
                }
                else
                {
                    Response.StatusCode = 401;
                    return BadRequest("You tried to delete an element that does not belong to you");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
