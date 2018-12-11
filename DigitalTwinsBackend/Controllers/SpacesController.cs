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

namespace DigitalTwinsBackend.Controllers
{
    public class SpacesController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private SpaceViewModel _model;
        private IMemoryCache _cache;

        public SpacesController(IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
        {
            _httpContextAccessor = httpContextAccessor;
            _cache = memoryCache;

            //_model = new SpaceViewModel(_cache);
        }

        #region List (overview) / Details
        public async Task<ActionResult> List()
        {
            var model = new SpacesViewModel(_cache);
            model.SpaceList = await DigitalTwinsHelper.GetRootSpacesAsync(_cache, Loggers.SilentLogger);

            return View(model);
        }

        public async Task<ActionResult> Details(Guid id)
        {
            _model = new SpaceViewModel(_cache);
            await _model.LoadAsync(id);
            return View(_model);
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
        public async Task<ActionResult> Create()
        {
            _model = new SpaceViewModel(_cache);
            await _model.LoadAsync();
            return View(_model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(SpaceViewModel model, string createButton)
        {
            _model = new SpaceViewModel(_cache);

            if (ModelState.IsValid)
            {
                //SpaceCreate spaceCreate = new SpaceCreate(ExtractSpaceFromModel(model, true));
                Space space = ExtractSpaceFromModel(model, true);

                try
                {
                    var id = await DigitalTwinsHelper.CreateSpaceAsync(space, _cache, Loggers.SilentLogger);
                    await FeedbackHelper.Channel.SendMessageAsync($"Space with id '{id}' successfully created.");

                    switch (createButton)
                    {
                        case "Save & Next":
                            return RedirectToAction(nameof(Create));
                        case "Save & Continue":
                            if (id != Guid.Empty)
                            {
                                return RedirectToAction(nameof(Edit), new { id = id });
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
                    await FeedbackHelper.Channel.SendMessageAsync(ex.Message);
                    _model = new SpaceViewModel(_cache);
                    return View(_model);
                }
            }
            else
            {
                return View("Create");
            }
        }
        #endregion
        
        #region Edit / Update
        public async Task<ActionResult> Edit(Guid id)
        {
            _model = new SpaceViewModel(_cache);
            await _model.LoadAsync(id);

            return View(_model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(SpaceViewModel model)
        {
            _model = new SpaceViewModel(_cache);
            await _model.LoadAsync(model.SelectedSpaceItem.Id);
            var space = ExtractSpaceFromModel(model, false);

            try
            {
                await DigitalTwinsHelper.UpdateSpaceAsync(space, _cache, Loggers.SilentLogger);
                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                await FeedbackHelper.Channel.SendMessageAsync(ex.Message);
                model = new SpaceViewModel(_cache, model.SelectedSpaceItem.Id);
                return View(model);
            }
        }
        #endregion

        #region Delete
        public async Task<ActionResult> Delete(Guid id)
        {
            _model = new SpaceViewModel(_cache);
            await _model.LoadAsync(id);

            return View(_model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(SpaceViewModel model)
        {
            try
            {
                if (await DigitalTwinsHelper.DeleteSpaceAsync(model.SelectedSpaceItem, _cache, Loggers.SilentLogger))
                {
                    return RedirectToAction(nameof(List));
                }
                else
                {
                    _model = new SpaceViewModel(_cache);
                    await _model.LoadAsync(model.SelectedSpaceItem.Id);

                    return View(_model);
                }
            }
            catch (Exception ex)
            {
                await FeedbackHelper.Channel.SendMessageAsync(ex.InnerException.ToString());
                return View();
            }
        }
        #endregion

        private Space ExtractSpaceFromModel(SpaceViewModel model, bool isInCreate)
        {
            Space space = model.SelectedSpaceItem;

            // conversion from Id
            space.TypeId = _model.SpaceTypeList.Single(t => t.Name.Equals(space.Type)).Id;
            space.SubTypeId = _model.SpaceSubTypeList.Single(t => t.Name.Equals(space.SubType)).Id;
            if (!isInCreate) { space.StatusId = _model.SpaceStatusList.Single(t => t.Name.Equals(space.Status)).Id; }

            return space;
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult RedirectSensorDetails(Guid id)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(SensorsController.Details));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }
}