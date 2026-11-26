using HouseRentalAPI.Constants;
using HouseRentalAPI.Exceptions;
using HouseRentalAPI.Models;
using HouseRentalAPI.Repositories;
using HouseRentalAPI.Services;
using Moq;

namespace HouseRentalAPI.Tests.Services;

/// <summary>Người dùng nhắn tin cho quản trị viên (bộ phận hỗ trợ)</summary>
public class ConversationServiceTests
{
    private const int TenantId = 10;

    private readonly Mock<IConversationRepository> _conversations = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPropertyRepository> _properties = new();

    private ConversationService CreateService(int userId, string role) =>
        new(_conversations.Object, _users.Object, _properties.Object, TestHelpers.CurrentUser(userId, role).Object);

    private void SetupSave(int conversationId = 50)
    {
        Conversation? added = null;
        _conversations.Setup(r => r.AddAsync(It.IsAny<Conversation>()))
            .Callback<Conversation>(c => { c.Id = conversationId; added = c; })
            .Returns(Task.CompletedTask);
        _conversations.Setup(r => r.GetDetailAsync(conversationId)).ReturnsAsync(() =>
        {
            added!.Initiator = new User { Id = added.InitiatorId, FullName = "Người thuê", Role = UserRole.Tenant };
            added.Recipient = new User { Id = added.RecipientId, FullName = "Quản trị viên", Role = UserRole.Admin };
            return added;
        });
    }

    [Fact]
    public async Task LienHeHoTro_GuiToiAdminDauTien_VaTraVeRoleAdmin()
    {
        _users.Setup(r => r.GetActiveAdminIdsAsync()).ReturnsAsync([1, 7]);
        _users.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, IsActive = true, Role = UserRole.Admin });
        SetupSave();

        var result = await CreateService(TenantId, AppRoles.Tenant).ContactSupportAsync("Cần hỗ trợ thanh toán");

        Assert.Equal(1, result.OtherParticipant.Id);
        Assert.Equal(UserRole.Admin, result.OtherParticipant.Role);
        Assert.Equal("Cần hỗ trợ thanh toán", result.LastMessage);
    }

    [Fact]
    public async Task LienHeHoTro_DaTungChatVoiAdminKhac_TiepTucCuocTroChuyenDo()
    {
        var existing = new Conversation { Id = 33, InitiatorId = TenantId, RecipientId = 7 };
        _users.Setup(r => r.GetActiveAdminIdsAsync()).ReturnsAsync([1, 7]);
        _users.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new User { Id = 7, IsActive = true, Role = UserRole.Admin });
        _conversations.Setup(r => r.FindBetweenAsync(TenantId, 7, null)).ReturnsAsync(existing);
        _conversations.Setup(r => r.GetDetailAsync(33)).ReturnsAsync(() =>
        {
            existing.Initiator = new User { Id = TenantId, FullName = "Người thuê" };
            existing.Recipient = new User { Id = 7, FullName = "Admin 2", Role = UserRole.Admin };
            return existing;
        });

        var result = await CreateService(TenantId, AppRoles.Tenant).ContactSupportAsync("Mình hỏi tiếp nhé");

        Assert.Equal(33, result.Id);
        Assert.Equal(7, result.OtherParticipant.Id);
        _conversations.Verify(r => r.AddAsync(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task LienHeHoTro_AdminTuGui_NemBadRequest() =>
        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService(1, AppRoles.Admin).ContactSupportAsync("test"));

    [Fact]
    public async Task LienHeHoTro_KhongCoAdmin_NemNotFound()
    {
        _users.Setup(r => r.GetActiveAdminIdsAsync()).ReturnsAsync([]);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService(TenantId, AppRoles.Tenant).ContactSupportAsync("test"));
    }
}
