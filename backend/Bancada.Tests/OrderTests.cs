using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bancada.Tests;

public class OrderTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    internal const string Tech1 = "00000000-0000-0000-0000-000000000002";
    internal static async Task<JsonElement> Create(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "Cliente Ficticio", customerEmail = "cliente@bancada.example",
            equipmentLabel = "Notebook Demo", equipmentSerial = "FICTICIO-001",
            reportedIssue = "Nao liga", technicianId = Tech1
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    internal static string Path(JsonElement order, string suffix = "") =>
        $"/api/orders/{order.GetProperty("id").GetString()}{suffix}";

    [Fact]
    public async Task Order_creation_detail_filters_and_validation()
    {
        using var staff = await fixture.Staff();
        var order = await Create(staff);
        Assert.Equal("received", order.GetProperty("status").GetString());
        Assert.Equal(1, order.GetProperty("version").GetInt32());
        Assert.Equal("received", order.GetProperty("history")[0].GetProperty("action").GetString());
        Assert.Equal("Tecnico Um", order.GetProperty("technicianName").GetString());
        var list = await staff.GetFromJsonAsync<JsonElement>("/api/orders?search=Notebook&status=received&pageSize=50");
        Assert.True(list.GetProperty("total").GetInt32() >= 1);
        foreach (var query in new[] { "status=invalid", "pageSize=51", "technicianId=invalid", "page=0", "extra=1" })
            Assert.Equal(HttpStatusCode.BadRequest, (await staff.GetAsync("/api/orders?" + query)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync("/api/orders",
            new { customerName = "", technicianId = Tech1 })).StatusCode);
    }

    [Fact]
    public async Task Order_diagnosis_is_assigned_technician_only()
    {
        using var staff = await fixture.Staff();
        using var other = await fixture.Staff("tecnico2@bancada.example");
        using var tech = await fixture.Staff("tecnico1@bancada.example");
        var order = await Create(staff);
        var request = new { expectedVersion = 1, diagnosis = "Fonte defeituosa" };
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync(Path(order, "/diagnosis"), request)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync(Path(order, "/diagnosis"), request)).StatusCode);
        var response = await tech.PostAsJsonAsync(Path(order, "/diagnosis"), request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("diagnosing", updated.GetProperty("status").GetString());
        Assert.Equal(2, updated.GetProperty("version").GetInt32());
        Assert.Equal(2, updated.GetProperty("history").GetArrayLength());
    }

    [Fact]
    public async Task Order_concurrent_versions_commit_exactly_one_event()
    {
        using var staff = await fixture.Staff();
        using var first = await fixture.Staff("tecnico1@bancada.example");
        using var second = await fixture.Staff("tecnico1@bancada.example");
        var order = await Create(staff);
        var responses = await Task.WhenAll(
            first.PostAsJsonAsync(Path(order, "/diagnosis"), new { expectedVersion = 1, diagnosis = "Primeira" }),
            second.PostAsJsonAsync(Path(order, "/diagnosis"), new { expectedVersion = 1, diagnosis = "Segunda" }));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        var stored = await staff.GetFromJsonAsync<JsonElement>(Path(order));
        Assert.Equal(2, stored.GetProperty("version").GetInt32());
        Assert.Equal(2, stored.GetProperty("history").GetArrayLength());
    }

    [Fact]
    public async Task Order_cancellation_reason_terminal_and_repair_guards()
    {
        using var staff = await fixture.Staff();
        using var tech = await fixture.Staff("tecnico1@bancada.example");
        var order = await Create(staff);
        Assert.Equal(HttpStatusCode.Conflict, (await tech.PostAsJsonAsync(Path(order, "/transitions"),
            new { expectedVersion = 1, target = "repairing" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync(Path(order, "/transitions"),
            new { expectedVersion = 1, target = "repairing" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsJsonAsync(Path(order, "/transitions"),
            new { expectedVersion = 1, target = "cancelled" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync(Path(order, "/transitions"),
            new { expectedVersion = 1, target = "cancelled", reason = "Cliente desistiu" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await tech.PostAsJsonAsync(Path(order, "/diagnosis"),
            new { expectedVersion = 2, diagnosis = "Nova" })).StatusCode);
    }
}
