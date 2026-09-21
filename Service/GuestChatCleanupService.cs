using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MenuGoBE.Data;
using MenuGoBE.Interface.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MenuGoBE.Service
{
    public class GuestChatCleanupService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GuestChatCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

        public GuestChatCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<GuestChatCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        #region BACKGROUND SERVICE Vòng lặp định kỳ dọn dẹp các phiên chat ẩn danh quá hạn
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình dọn dẹp dữ liệu chat của khách ẩn danh.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
        #endregion

        #region CLEANUP Thực hiện quét và dọn dẹp các cuộc hội thoại và phiên khách ẩn danh hết hạn
        private async Task CleanupAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var branches = await context.Branches.ToListAsync();
            var totalCleaned = 0;

            foreach (var branch in branches)
            {
                var retentionMinutes = branch.AnonymousChatRetentionMinutes > 0 ? branch.AnonymousChatRetentionMinutes : 43200;
                var cutoffTime = DateTime.UtcNow.AddMinutes(-retentionMinutes);

                // Lấy danh sách các cuộc trò chuyện của khách ẩn danh thuộc chi nhánh có UpdatedAt < cutoffTime
                var expiredConversations = await context.Conversations
                    .Where(c => c.BranchId == branch.Id && c.GuestChatSessionId.HasValue && c.UpdatedAt < cutoffTime)
                    .ToListAsync();

                if (expiredConversations.Any())
                {
                    var convIds = expiredConversations.Select(c => c.Id).ToList();

                    var histories = await context.ConversationAssignmentHistories
                        .Where(h => convIds.Contains(h.ConversationId))
                        .ToListAsync();
                    context.ConversationAssignmentHistories.RemoveRange(histories);

                    var messages = await context.Messages
                        .Where(m => convIds.Contains(m.ConversationId))
                        .ToListAsync();
                    context.Messages.RemoveRange(messages);

                    context.Conversations.RemoveRange(expiredConversations);
                    totalCleaned += expiredConversations.Count;
                }
            }

            // Dọn dẹp các GuestChatSession không còn cuộc hội thoại nào và LastActiveTime quá 30 ngày
            var sessionCutoff = DateTime.UtcNow.AddDays(-30);
            var orphanSessions = await context.GuestChatSessions
                .Where(g => g.LastActiveTime < sessionCutoff && !g.Conversations.Any())
                .ToListAsync();

            if (orphanSessions.Any())
            {
                context.GuestChatSessions.RemoveRange(orphanSessions);
            }

            if (totalCleaned > 0 || orphanSessions.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Đã dọn dẹp {Count} cuộc trò chuyện và {SessionCount} phiên khách ẩn danh hết hạn.", totalCleaned, orphanSessions.Count);
            }
        }
        #endregion
    }
}
