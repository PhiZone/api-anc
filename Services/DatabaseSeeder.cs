using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using PhiZoneApi.Constants;
using PhiZoneApi.Data;
using PhiZoneApi.Models;
// ReSharper disable StringLiteralTypo

namespace PhiZoneApi.Services;

public class DatabaseSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await PopulateRoles(scope);
        await PopulateRegions(scope, cancellationToken);
        await PopulateScopes(scope, cancellationToken);
        await PopulateInternalApps(scope, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task PopulateRoles(IServiceScope scope)
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var roles = new List<string>
        {
            Roles.Member,
            Roles.Qualified,
            Roles.Volunteer,
            Roles.Moderator,
            Roles.Administrator
        };
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new Role { Name = role });
    }

    private async Task PopulateScopes(IServiceScope scope, CancellationToken cancellationToken)
    {
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var scopeDescriptor = new OpenIddictScopeDescriptor { Name = "basic_access" };
        var scopeInstance = await scopeManager.FindByNameAsync(scopeDescriptor.Name, cancellationToken);
        if (scopeInstance == null)
            await scopeManager.CreateAsync(scopeDescriptor, cancellationToken);
        else
            await scopeManager.UpdateAsync(scopeInstance, scopeDescriptor, cancellationToken);
    }

    private async Task PopulateInternalApps(IServiceScope scopeService, CancellationToken cancellationToken)
    {
        var appManager = scopeService.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var appDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "regular",
            ClientSecret = "c29b1587-80f9-475f-b97b-dca1884eb0e3",
            Type = OpenIddictConstants.ClientTypes.Confidential,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Introspection,
                OpenIddictConstants.Permissions.Endpoints.Revocation,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.Prefixes.Scope + "basic_access"
            }
        };
        var client = await appManager.FindByClientIdAsync(appDescriptor.ClientId, cancellationToken);
        if (client == null)
            await appManager.CreateAsync(appDescriptor, cancellationToken);
        else
            await appManager.UpdateAsync(client, appDescriptor, cancellationToken);
    }

    private async Task PopulateRegions(IServiceScope scope, CancellationToken cancellationToken)
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var regions = new Dictionary<string, string>
        {
            { "AF", "Afghanistan" },
            { "AX", "Åland Islands" },
            { "AL", "Albania" },
            { "DZ", "Algeria" },
            { "AS", "American Samoa" },
            { "AD", "Andorra" },
            { "AO", "Angola" },
            { "AI", "Anguilla" },
            { "AQ", "Antarctica" },
            { "AG", "Antigua and Barbuda" },
            { "AR", "Argentina" },
            { "AM", "Armenia" },
            { "AW", "Aruba" },
            { "AU", "Australia" },
            { "AT", "Austria" },
            { "AZ", "Azerbaijan" },
            { "BS", "Bahamas" },
            { "BH", "Bahrain" },
            { "BD", "Bangladesh" },
            { "BB", "Barbados" },
            { "BY", "Belarus" },
            { "BE", "Belgium" },
            { "BZ", "Belize" },
            { "BJ", "Benin" },
            { "BM", "Bermuda" },
            { "BT", "Bhutan" },
            { "BO", "Bolivia" },
            { "BA", "Bosnia and Herzegovina" },
            { "BW", "Botswana" },
            { "BV", "Bouvet Island" },
            { "BR", "Brazil" },
            { "IO", "British Indian Ocean Territory" },
            { "VG", "British Virgin Islands" },
            { "BN", "Brunei" },
            { "BG", "Bulgaria" },
            { "BF", "Burkina Faso" },
            { "BI", "Burundi" },
            { "KH", "Cambodia" },
            { "CM", "Cameroon" },
            { "CA", "Canada" },
            { "CV", "Cape Verde" },
            { "BQ", "Caribbean Netherlands" },
            { "KY", "Cayman Islands" },
            { "CF", "Central African Republic" },
            { "TD", "Chad" },
            { "CL", "Chile" },
            { "CN", "China" },
            { "CX", "Christmas Island" },
            { "CC", "Cocos (Keeling) Islands" },
            { "CO", "Colombia" },
            { "KM", "Comoros" },
            { "CG", "Congo - Brazzaville" },
            { "CD", "Congo - Kinshasa" },
            { "CK", "Cook Islands" },
            { "CR", "Costa Rica" },
            { "CI", "Côte d’Ivoire" },
            { "HR", "Croatia" },
            { "CU", "Cuba" },
            { "CW", "Curaçao" },
            { "CY", "Cyprus" },
            { "CZ", "Czech Republic" },
            { "DK", "Denmark" },
            { "DJ", "Djibouti" },
            { "DM", "Dominica" },
            { "DO", "Dominican Republic" },
            { "EC", "Ecuador" },
            { "EG", "Egypt" },
            { "SV", "El Salvador" },
            { "GQ", "Equatorial Guinea" },
            { "ER", "Eritrea" },
            { "EE", "Estonia" },
            { "ET", "Ethiopia" },
            { "FK", "Falkland Islands" },
            { "FO", "Faroe Islands" },
            { "FJ", "Fiji" },
            { "FI", "Finland" },
            { "FR", "France" },
            { "GF", "French Guiana" },
            { "PF", "French Polynesia" },
            { "TF", "French Southern Territories" },
            { "GA", "Gabon" },
            { "GM", "Gambia" },
            { "GE", "Georgia" },
            { "DE", "Germany" },
            { "GH", "Ghana" },
            { "GI", "Gibraltar" },
            { "GR", "Greece" },
            { "GL", "Greenland" },
            { "GD", "Grenada" },
            { "GP", "Guadeloupe" },
            { "GU", "Guam" },
            { "GT", "Guatemala" },
            { "GG", "Guernsey" },
            { "GN", "Guinea" },
            { "GW", "Guinea-Bissau" },
            { "GY", "Guyana" },
            { "HT", "Haiti" },
            { "HM", "Heard and McDonald Islands" },
            { "HN", "Honduras" },
            { "HK", "Hong Kong SAR China" },
            { "HU", "Hungary" },
            { "IS", "Iceland" },
            { "IN", "India" },
            { "ID", "Indonesia" },
            { "IR", "Iran" },
            { "IQ", "Iraq" },
            { "IE", "Ireland" },
            { "IM", "Isle of Man" },
            { "IL", "Israel" },
            { "IT", "Italy" },
            { "JM", "Jamaica" },
            { "JP", "Japan" },
            { "JE", "Jersey" },
            { "JO", "Jordan" },
            { "KZ", "Kazakhstan" },
            { "KE", "Kenya" },
            { "KI", "Kiribati" },
            { "KW", "Kuwait" },
            { "KG", "Kyrgyzstan" },
            { "LA", "Laos" },
            { "LV", "Latvia" },
            { "LB", "Lebanon" },
            { "LS", "Lesotho" },
            { "LR", "Liberia" },
            { "LY", "Libya" },
            { "LI", "Liechtenstein" },
            { "LT", "Lithuania" },
            { "LU", "Luxembourg" },
            { "MO", "Macau SAR China" },
            { "MK", "Macedonia" },
            { "MG", "Madagascar" },
            { "MW", "Malawi" },
            { "MY", "Malaysia" },
            { "MV", "Maldives" },
            { "ML", "Mali" },
            { "MT", "Malta" },
            { "MH", "Marshall Islands" },
            { "MQ", "Martinique" },
            { "MR", "Mauritania" },
            { "MU", "Mauritius" },
            { "YT", "Mayotte" },
            { "MX", "Mexico" },
            { "FM", "Micronesia" },
            { "MD", "Moldova" },
            { "MC", "Monaco" },
            { "MN", "Mongolia" },
            { "ME", "Montenegro" },
            { "MS", "Montserrat" },
            { "MA", "Morocco" },
            { "MZ", "Mozambique" },
            { "MM", "Myanmar (Burma)" },
            { "NA", "Namibia" },
            { "NR", "Nauru" },
            { "NP", "Nepal" },
            { "NL", "Netherlands" },
            { "NC", "New Caledonia" },
            { "NZ", "New Zealand" },
            { "NI", "Nicaragua" },
            { "NE", "Niger" },
            { "NG", "Nigeria" },
            { "NU", "Niue" },
            { "NF", "Norfolk Island" },
            { "MP", "Northern Mariana Islands" },
            { "KP", "North Korea" },
            { "NO", "Norway" },
            { "OM", "Oman" },
            { "PK", "Pakistan" },
            { "PW", "Palau" },
            { "PS", "Palestinian Territories" },
            { "PA", "Panama" },
            { "PG", "Papua New Guinea" },
            { "PY", "Paraguay" },
            { "PE", "Peru" },
            { "PH", "Philippines" },
            { "PN", "Pitcairn Islands" },
            { "PL", "Poland" },
            { "PT", "Portugal" },
            { "PR", "Puerto Rico" },
            { "QA", "Qatar" },
            { "RE", "Réunion" },
            { "RO", "Romania" },
            { "RU", "Russia" },
            { "RW", "Rwanda" },
            { "BL", "Saint Barthélemy" },
            { "SH", "Saint Helena" },
            { "KN", "Saint Kitts and Nevis" },
            { "LC", "Saint Lucia" },
            { "MF", "Saint Martin" },
            { "PM", "Saint Pierre and Miquelon" },
            { "WS", "Samoa" },
            { "SM", "San Marino" },
            { "ST", "São Tomé and Príncipe" },
            { "SA", "Saudi Arabia" },
            { "SN", "Senegal" },
            { "RS", "Serbia" },
            { "SC", "Seychelles" },
            { "SL", "Sierra Leone" },
            { "SG", "Singapore" },
            { "SX", "Sint Maarten" },
            { "SK", "Slovakia" },
            { "SI", "Slovenia" },
            { "SB", "Solomon Islands" },
            { "SO", "Somalia" },
            { "ZA", "South Africa" },
            { "GS", "South Georgia and the South Sandwich Islands" },
            { "KR", "South Korea" },
            { "SS", "South Sudan" },
            { "ES", "Spain" },
            { "LK", "Sri Lanka" },
            { "VC", "St. Vincent and the Grenadines" },
            { "SD", "Sudan" },
            { "SR", "Suriname" },
            { "SJ", "Svalbard and Jan Mayen" },
            { "SZ", "Swaziland" },
            { "SE", "Sweden" },
            { "CH", "Switzerland" },
            { "SY", "Syria" },
            { "TW", "Taiwan Prov. China" },
            { "TJ", "Tajikistan" },
            { "TZ", "Tanzania" },
            { "TH", "Thailand" },
            { "TL", "Timor-Leste" },
            { "TG", "Togo" },
            { "TK", "Tokelau" },
            { "TO", "Tonga" },
            { "TT", "Trinidad and Tobago" },
            { "TN", "Tunisia" },
            { "TR", "Turkey" },
            { "TM", "Turkmenistan" },
            { "TC", "Turks and Caicos Islands" },
            { "TV", "Tuvalu" },
            { "UG", "Uganda" },
            { "UA", "Ukraine" },
            { "AE", "United Arab Emirates" },
            { "GB", "United Kingdom" },
            { "UY", "Uruguay" },
            { "US", "United States" },
            { "UM", "U.S. Outlying Islands" },
            { "VI", "U.S. Virgin Islands" },
            { "UZ", "Uzbekistan" },
            { "VU", "Vanuatu" },
            { "VA", "Vatican City" },
            { "VE", "Venezuela" },
            { "VN", "Vietnam" },
            { "WF", "Wallis and Futuna" },
            { "EH", "Western Sahara" },
            { "YE", "Yemen" },
            { "ZM", "Zambia" },
            { "ZW", "Zimbabwe" }
        };
        if (await context.Regions.CountAsync(cancellationToken) >= regions.Count) return;
        foreach (var region in regions.Select(entry => new Region { Code = entry.Key, Name = entry.Value }))
        {
            if (await context.Regions.AnyAsync(r => string.Equals(r.Code, region.Code),
                    cancellationToken))
                continue;

            await context.Regions.AddAsync(region, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}