﻿using System.Linq;
using DDD.Light.Messaging;
using DDD.Light.Messaging.InProcess;
using DDD.Light.Realtor.API.Commands.Realtor;
using DDD.Light.Realtor.Core.Domain.Model;
using DDD.Light.Realtor.Core.Domain.Model.Listing;
using DDD.Light.Realtor.Core.Domain.Model.Listing.AggregateRoot;
using DDD.Light.Repo.Contracts;

namespace DDD.Light.Realtor.Application.CommandHandlers.Realtor
{
    public class PostListingHandler : CommandHandler<PostListing>
    {
        private readonly IRepository<Core.Domain.Model.Realtor.AggregateRoot.Realtor> _realtorRepo;

        public PostListingHandler(IRepository<Core.Domain.Model.Realtor.AggregateRoot.Realtor> realtorRepo)
        {
            _realtorRepo = realtorRepo;
        }

        public override void Handle(PostListing command)
        {
            var listing = new Listing
                {
                    Id = command.ListingId,
                    Active = true,
                    Description = new Description
                        {
                            NumberOfBathrooms = command.NumberOfBathrooms,
                            NumberOfBedrooms = command.NumberOfBedrooms,
                            YearBuilt = command.YearBuilt
                        },
                        Location = new Location
                            {
                                City = command.City,
                                Street = command.Street,
                                State = command.State,
                                Zip = command.Zip
                            }
                };
            var realtor = _realtorRepo.Get().First();
            realtor.PostListing(listing);
        }
    }
}