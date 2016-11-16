﻿using System;
using System.Threading.Tasks;
using AllReady.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Geocoding;
using System.Linq;
using AllReady.Extensions;

namespace AllReady.Areas.Admin.Features.Requests
{
    public class EditRequestCommandHandler : IAsyncRequestHandler<EditRequestCommand, Guid>
    {
        private readonly AllReadyContext _context;
        private readonly IGeocoder _geocoder;

        public EditRequestCommandHandler(AllReadyContext context, IGeocoder geocoder)
        {
            _context = context;
            _geocoder = geocoder;
        }

        public async Task<Guid> Handle(EditRequestCommand message)
        {
            var request = await _context.Requests
                .Include(l => l.Event)
                .SingleOrDefaultAsync(t => t.RequestId == message.RequestModel.Id) ?? _context.Add(new Request { Source = RequestSource.UI }).Entity;

            request.EventId = message.RequestModel.EventId;
            request.Address = message.RequestModel.Address;
            request.City = message.RequestModel.City;
            request.Name = message.RequestModel.Name;
            request.State = message.RequestModel.State;
            request.Zip = message.RequestModel.Zip;
            request.Email = message.RequestModel.Email;
            request.Phone = message.RequestModel.Phone;

            //If lat/long not provided, use geocoding API to get them
            //Also, always update lat/long to be safe if this is an update operation since it is unknown whether the address changed or not.
            if ((request.Latitude == 0 && request.Longitude == 0) || request.RequestId != Guid.Empty)
            {
                //Assume the first returned address is correct
                var address = _geocoder.Geocode(request.Address, request.City, request.State, request.Zip, string.Empty)
                    .FirstOrDefault();
                request.Latitude = address?.Coordinates.Latitude ?? 0;
                request.Longitude = address?.Coordinates.Longitude ?? 0;
            }

            _context.AddOrUpdate(request);

            await _context.SaveChangesAsync();

            return request.RequestId;
        }
    }
}
