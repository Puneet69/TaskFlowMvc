using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskFlowMvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlatformUpgradeAuthRolesTeamsProjectsTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerId",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "InviteRole",
                table: "TeamInvitations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "TaskItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTemplateBased",
                table: "TaskItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LabelsCsv",
                table: "TaskItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ParentTaskId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                table: "TaskItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceType",
                table: "TaskItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecursUntilUtc",
                table: "TaskItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaskTemplateId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DisabledAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisabledReason",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeviceSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SessionKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoginActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EmailAttempted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginActivities_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationItems_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMilestones_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskItemId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MentionedUserIdsCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskComments_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskComments_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    DependsOnTaskId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_TaskItems_DependsOnTaskId",
                        column: x => x.DependsOnTaskId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_TaskItems_TaskId",
                        column: x => x.TaskId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    LabelsCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamId = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamActivityLogs_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeamActivityLogs_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InvitedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInvitations_AspNetUsers_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommentReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskCommentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Emoji = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommentReactions_TaskComments_TaskCommentId",
                        column: x => x.TaskCommentId,
                        principalTable: "TaskComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskItemId = table.Column<int>(type: "int", nullable: true),
                    TaskCommentId = table.Column<int>(type: "int", nullable: true),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAttachments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileAttachments_TaskComments_TaskCommentId",
                        column: x => x.TaskCommentId,
                        principalTable: "TaskComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileAttachments_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ParentTaskId",
                table: "TaskItems",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectId_Status_DueDate",
                table: "TaskItems",
                columns: new[] { "ProjectId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TaskTemplateId",
                table: "TaskItems",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId_Status_IsArchived",
                table: "Projects",
                columns: new[] { "OwnerId", "Status", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TeamId",
                table: "Projects",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentReactions_TaskCommentId_UserId_Emoji",
                table: "CommentReactions",
                columns: new[] { "TaskCommentId", "UserId", "Emoji" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentReactions_UserId",
                table: "CommentReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSessions_SessionKey",
                table: "DeviceSessions",
                column: "SessionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceSessions_UserId_RevokedAtUtc",
                table: "DeviceSessions",
                columns: new[] { "UserId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_TaskCommentId",
                table: "FileAttachments",
                column: "TaskCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_TaskItemId_FileName_Version",
                table: "FileAttachments",
                columns: new[] { "TaskItemId", "FileName", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachments_UploadedByUserId",
                table: "FileAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginActivities_UserId_OccurredAtUtc",
                table: "LoginActivities",
                columns: new[] { "UserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationItems_UserId_IsRead_CreatedAtUtc",
                table: "NotificationItems",
                columns: new[] { "UserId", "IsRead", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMilestones_ProjectId_DueDate",
                table: "ProjectMilestones",
                columns: new[] { "ProjectId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_AuthorUserId",
                table: "TaskComments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComments_TaskItemId",
                table: "TaskComments",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_DependsOnTaskId",
                table: "TaskDependencies",
                column: "DependsOnTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_TaskId_DependsOnTaskId",
                table: "TaskDependencies",
                columns: new[] { "TaskId", "DependsOnTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_OwnerId_Name",
                table: "TaskTemplates",
                columns: new[] { "OwnerId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_TeamActivityLogs_ActorUserId",
                table: "TeamActivityLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamActivityLogs_TeamId_CreatedAtUtc",
                table: "TeamActivityLogs",
                columns: new[] { "TeamId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_Email_IsAccepted",
                table: "UserInvitations",
                columns: new[] { "Email", "IsAccepted" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_InvitedById",
                table: "UserInvitations",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_Token",
                table: "UserInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Teams_TeamId",
                table: "Projects",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskItems_ParentTaskId",
                table: "TaskItems",
                column: "ParentTaskId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskTemplates_TaskTemplateId",
                table: "TaskItems",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Teams_TeamId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskItems_ParentTaskId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskTemplates_TaskTemplateId",
                table: "TaskItems");

            migrationBuilder.DropTable(
                name: "CommentReactions");

            migrationBuilder.DropTable(
                name: "DeviceSessions");

            migrationBuilder.DropTable(
                name: "FileAttachments");

            migrationBuilder.DropTable(
                name: "LoginActivities");

            migrationBuilder.DropTable(
                name: "NotificationItems");

            migrationBuilder.DropTable(
                name: "ProjectMilestones");

            migrationBuilder.DropTable(
                name: "TaskDependencies");

            migrationBuilder.DropTable(
                name: "TaskTemplates");

            migrationBuilder.DropTable(
                name: "TeamActivityLogs");

            migrationBuilder.DropTable(
                name: "UserInvitations");

            migrationBuilder.DropTable(
                name: "TaskComments");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ParentTaskId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectId_Status_DueDate",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_TaskTemplateId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerId_Status_IsArchived",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TeamId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "InviteRole",
                table: "TeamInvitations");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "IsTemplateBased",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "LabelsCsv",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "RecurrenceType",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "RecursUntilUtc",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "TaskTemplateId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DisabledAtUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DisabledReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectId",
                table: "TaskItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId",
                table: "Projects",
                column: "OwnerId");
        }
    }
}
