﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;

using DigitalTwinsBackend.Helpers;
using DigitalTwinsBackend.Hubs;
using DigitalTwinsBackend.Models;
using DigitalTwinsBackend.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DigitalTwinsBackend.Controllers
{
    public class SpaceController : BaseController
    {
        public SpaceController(IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache = memoryCache;
        }

        #region List (overview) / Details
        [HttpGet]
        public async Task<ActionResult> List()
        {
            CacheHelper.SetPreviousPage(_cache, Request.Headers["Referer"].ToString());
            var model = new SpacesViewModel(_cache);

            try
            {
                model.SpaceList = await DigitalTwinsHelper.GetRootSpacesAsync(_cache, Loggers.SilentLogger, false);
            }
            catch (Exception ex)
            {
                FeedbackHelper.Channel.SendMessageAsync($"Error - {ex.Message}", MessageType.Info).Wait();
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult Details(Guid id)
        {
            CacheHelper.SetPreviousPage(_cache, Request.Headers["Referer"].ToString());

            SpaceViewModel model = new SpaceViewModel(_cache, id);
            return View(model);
        }
        #endregion

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Search(SpacesViewModel model)
        {
            var searchString = model.SearchString;
            var searchType = model.SearchType;

            model = new SpacesViewModel(_cache);

            int searchTypeId = searchType.Equals("All") ? -1 : model.SpaceTypeList.Single(t => t.Name.Equals(searchType)).Id;

            model.SpaceList = await DigitalTwinsHelper.SearchSpacesAsync(_cache, Loggers.SilentLogger, searchString, searchTypeId);
            return View("List", model);
        }


        #region Create
        [HttpGet]
        public ActionResult Create()
        {
            CacheHelper.SetPreviousPage(_cache, Request.Headers["Referer"].ToString());
            SpaceViewModel model = new SpaceViewModel(_cache);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(SpaceViewModel model, string createButton)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var spaceResult = await DigitalTwinsHelper.CreateSpaceAsync(model.SelectedSpaceItem, _cache, Loggers.SilentLogger);

                    switch (createButton)
                    {
                        case "Save & Close":
                            return Redirect(CacheHelper.GetPreviousPage(_cache));
                        case "Save & Create another":
                            return RedirectToAction(nameof(Create));
                        case "Save & Edit":
                            if (spaceResult.Id != Guid.Empty)
                            {
                                return RedirectToAction(nameof(Edit), new { id = spaceResult.Id });
                            }
                            else
                            {
                                return RedirectToAction(nameof(List));
                            }
                        default:
                            return RedirectToAction(nameof(List));
                    }
                }
                catch (Exception ex)
                {
                    await FeedbackHelper.Channel.SendMessageAsync(ex.Message, MessageType.Info);
                    model = new SpaceViewModel(_cache);
                    return View(model);
                }
            }
            else
            {
                return View("Create");
            }
        }
        #endregion

        #region Edit / Update
        [HttpGet]
        public ActionResult Edit(Guid id)
        {
            CacheHelper.SetPreviousPage(_cache, Request.Headers["Referer"].ToString());

            SpaceViewModel model = new SpaceViewModel(_cache, id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(SpaceViewModel model)
        {
            try
            {
                await DigitalTwinsHelper.UpdateSpaceAsync(model.SelectedSpaceItem, _cache, Loggers.SilentLogger);
                return Redirect(CacheHelper.GetPreviousPage(_cache));
            }
            catch (Exception ex)
            {
                await FeedbackHelper.Channel.SendMessageAsync(ex.Message, MessageType.Info);
                model = new SpaceViewModel(_cache, model.SelectedSpaceItem.Id);
                return View(model);
            }
        }
        #endregion

        #region Delete
        [HttpGet]
        public ActionResult Delete(Guid id)
        {
            CacheHelper.SetPreviousPage(_cache, Request.Headers["Referer"].ToString());

            SpaceViewModel model = new SpaceViewModel(_cache, id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(SpaceViewModel model)
        {
            try
            {
                if (await DigitalTwinsHelper.DeleteSpaceAsync(model.SelectedSpaceItem, _cache, Loggers.SilentLogger))
                {
                    return Redirect(CacheHelper.GetPreviousPage(_cache));
                }
                else
                {
                    model = new SpaceViewModel(_cache, model.SelectedSpaceItem.Id);

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                await FeedbackHelper.Channel.SendMessageAsync(ex.InnerException.ToString(), MessageType.Info);
                return View();
            }
        }
        #endregion
    }
}