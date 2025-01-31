﻿using Bit.Core.Context;
using Bit.Core.Entities;
using Bit.Core.Entities.Provider;
using Bit.Core.Enums;
using Bit.Core.Models.Data;
using Bit.Core.Models.Data.Organizations;
using Bit.Core.Services;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Bit.Test.Common.Helpers;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.Services;

[SutProviderCustomize]
public class EventServiceTests
{
    public static IEnumerable<object[]> InstallationIdTestCases => TestCaseHelper.GetCombinationsOfMultipleLists(
        new object[] { Guid.NewGuid(), null },
        Enum.GetValues<EventType>().Select(e => (object)e)
    ).Select(p => p.ToArray());

    [Theory]
    [BitMemberAutoData(nameof(InstallationIdTestCases))]
    public async Task LogOrganizationEvent_ProvidesInstallationId(Guid? installationId, EventType eventType,
        Organization organization, SutProvider<EventService> sutProvider)
    {
        organization.Enabled = true;
        organization.UseEvents = true;

        sutProvider.GetDependency<ICurrentContext>().InstallationId.Returns(installationId);

        await sutProvider.Sut.LogOrganizationEventAsync(organization, eventType);

        await sutProvider.GetDependency<IEventWriteService>().Received(1).CreateAsync(Arg.Is<IEvent>(e =>
            e.OrganizationId == organization.Id &&
            e.Type == eventType &&
            e.InstallationId == installationId));
    }

    [Theory, BitAutoData]
    public async Task LogOrganizationUserEvent_LogsRequiredInfo(OrganizationUser orgUser, EventType eventType, DateTime date,
        Guid actingUserId, Guid providerId, string ipAddress, DeviceType deviceType, SutProvider<EventService> sutProvider)
    {
        var orgAbilities = new Dictionary<Guid, OrganizationAbility>()
        {
            {orgUser.OrganizationId, new OrganizationAbility() { UseEvents = true, Enabled = true } }
        };
        sutProvider.GetDependency<IApplicationCacheService>().GetOrganizationAbilitiesAsync().Returns(orgAbilities);
        sutProvider.GetDependency<ICurrentContext>().UserId.Returns(actingUserId);
        sutProvider.GetDependency<ICurrentContext>().IpAddress.Returns(ipAddress);
        sutProvider.GetDependency<ICurrentContext>().DeviceType.Returns(deviceType);
        sutProvider.GetDependency<ICurrentContext>().ProviderIdForOrg(Arg.Any<Guid>()).Returns(providerId);

        await sutProvider.Sut.LogOrganizationUserEventAsync(orgUser, eventType, date);

        var expected = new List<IEvent>() {
            new EventMessage()
            {
                IpAddress = ipAddress,
                DeviceType = deviceType,
                OrganizationId = orgUser.OrganizationId,
                UserId = orgUser.UserId,
                OrganizationUserId = orgUser.Id,
                ProviderId = providerId,
                Type = eventType,
                ActingUserId = actingUserId,
                Date = date
            }
        };

        await sutProvider.GetDependency<IEventWriteService>().Received(1).CreateManyAsync(Arg.Is(AssertHelper.AssertPropertyEqual<IEvent>(expected, new[] { "IdempotencyId" })));
    }

    [Theory, BitAutoData]
    public async Task LogProviderUserEvent_LogsRequiredInfo(ProviderUser providerUser, EventType eventType, DateTime date,
        Guid actingUserId, Guid providerId, string ipAddress, DeviceType deviceType, SutProvider<EventService> sutProvider)
    {
        var providerAbilities = new Dictionary<Guid, ProviderAbility>()
        {
            {providerUser.ProviderId, new ProviderAbility() { UseEvents = true, Enabled = true } }
        };
        sutProvider.GetDependency<IApplicationCacheService>().GetProviderAbilitiesAsync().Returns(providerAbilities);
        sutProvider.GetDependency<ICurrentContext>().UserId.Returns(actingUserId);
        sutProvider.GetDependency<ICurrentContext>().IpAddress.Returns(ipAddress);
        sutProvider.GetDependency<ICurrentContext>().DeviceType.Returns(deviceType);
        sutProvider.GetDependency<ICurrentContext>().ProviderIdForOrg(Arg.Any<Guid>()).Returns(providerId);

        await sutProvider.Sut.LogProviderUserEventAsync(providerUser, eventType, date);

        var expected = new List<IEvent>() {
            new EventMessage()
            {
                IpAddress = ipAddress,
                DeviceType = deviceType,
                ProviderId = providerUser.ProviderId,
                UserId = providerUser.UserId,
                ProviderUserId = providerUser.Id,
                Type = eventType,
                ActingUserId = actingUserId,
                Date = date
            }
        };

        await sutProvider.GetDependency<IEventWriteService>().Received(1).CreateManyAsync(Arg.Is(AssertHelper.AssertPropertyEqual<IEvent>(expected, new[] { "IdempotencyId" })));
    }
}
