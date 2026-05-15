using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exam_papers_tenants_TenantId",
                table: "exam_papers");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_questions_exam_papers_ExamPaperId",
                table: "exam_questions");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_questions_questions_QuestionId",
                table: "exam_questions");

            migrationBuilder.DropForeignKey(
                name: "FK_questions_tenants_TenantId",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "FK_student_answers_exam_papers_ExamPaperId",
                table: "student_answers");

            migrationBuilder.DropForeignKey(
                name: "FK_student_answers_questions_QuestionId",
                table: "student_answers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tenants",
                table: "tenants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_student_answers",
                table: "student_answers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_questions",
                table: "questions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_exam_questions",
                table: "exam_questions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_exam_papers",
                table: "exam_papers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ai_audit_logs",
                table: "ai_audit_logs");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "tenants",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tenants",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "tenants",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "SchemaName",
                table: "tenants",
                newName: "schema_name");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "tenants",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "tenants",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ContactEmail",
                table: "tenants",
                newName: "contact_email");

            migrationBuilder.RenameColumn(
                name: "AiCallUsed",
                table: "tenants",
                newName: "ai_call_used");

            migrationBuilder.RenameColumn(
                name: "AiCallQuota",
                table: "tenants",
                newName: "ai_call_quota");

            migrationBuilder.RenameIndex(
                name: "IX_tenants_SchemaName",
                table: "tenants",
                newName: "ix_tenants_schema_name");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "student_answers",
                newName: "score");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "student_answers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "student_answers",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "SubmittedAt",
                table: "student_answers",
                newName: "submitted_at");

            migrationBuilder.RenameColumn(
                name: "StudentId",
                table: "student_answers",
                newName: "student_id");

            migrationBuilder.RenameColumn(
                name: "QuestionId",
                table: "student_answers",
                newName: "question_id");

            migrationBuilder.RenameColumn(
                name: "GradingStatus",
                table: "student_answers",
                newName: "grading_status");

            migrationBuilder.RenameColumn(
                name: "ExamPaperId",
                table: "student_answers",
                newName: "exam_paper_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "student_answers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AnswerContent",
                table: "student_answers",
                newName: "answer_content");

            migrationBuilder.RenameColumn(
                name: "AiFeedback",
                table: "student_answers",
                newName: "ai_feedback");

            migrationBuilder.RenameIndex(
                name: "IX_student_answers_QuestionId",
                table: "student_answers",
                newName: "ix_student_answers_question_id");

            migrationBuilder.RenameIndex(
                name: "IX_student_answers_ExamPaperId_StudentId_QuestionId",
                table: "student_answers",
                newName: "ix_student_answers_exam_paper_id_student_id_question_id");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "questions",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Options",
                table: "questions",
                newName: "options");

            migrationBuilder.RenameColumn(
                name: "Explanation",
                table: "questions",
                newName: "explanation");

            migrationBuilder.RenameColumn(
                name: "Embedding",
                table: "questions",
                newName: "embedding");

            migrationBuilder.RenameColumn(
                name: "Difficulty",
                table: "questions",
                newName: "difficulty");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "questions",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "questions",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "questions",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "questions",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "KnowledgePoint",
                table: "questions",
                newName: "knowledge_point");

            migrationBuilder.RenameColumn(
                name: "IsAiGenerated",
                table: "questions",
                newName: "is_ai_generated");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "questions",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "questions",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CorrectAnswer",
                table: "questions",
                newName: "correct_answer");

            migrationBuilder.RenameIndex(
                name: "IX_questions_TenantId_IsActive",
                table: "questions",
                newName: "ix_questions_tenant_id_is_active");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "exam_questions",
                newName: "score");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "exam_questions",
                newName: "order");

            migrationBuilder.RenameColumn(
                name: "QuestionId",
                table: "exam_questions",
                newName: "question_id");

            migrationBuilder.RenameColumn(
                name: "ExamPaperId",
                table: "exam_questions",
                newName: "exam_paper_id");

            migrationBuilder.RenameIndex(
                name: "IX_exam_questions_QuestionId",
                table: "exam_questions",
                newName: "ix_exam_questions_question_id");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "exam_papers",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "exam_papers",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "exam_papers",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "exam_papers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "exam_papers",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TotalScore",
                table: "exam_papers",
                newName: "total_score");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "exam_papers",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "exam_papers",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "exam_papers",
                newName: "end_time");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "exam_papers",
                newName: "duration_minutes");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "exam_papers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AntiCheatingEnabled",
                table: "exam_papers",
                newName: "anti_cheating_enabled");

            migrationBuilder.RenameIndex(
                name: "IX_exam_papers_TenantId",
                table: "exam_papers",
                newName: "ix_exam_papers_tenant_id");

            migrationBuilder.RenameColumn(
                name: "Operation",
                table: "ai_audit_logs",
                newName: "operation");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ai_audit_logs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ai_audit_logs",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TotalTokens",
                table: "ai_audit_logs",
                newName: "total_tokens");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "ai_audit_logs",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "RelatedEntityId",
                table: "ai_audit_logs",
                newName: "related_entity_id");

            migrationBuilder.RenameColumn(
                name: "PromptTokens",
                table: "ai_audit_logs",
                newName: "prompt_tokens");

            migrationBuilder.RenameColumn(
                name: "ModelName",
                table: "ai_audit_logs",
                newName: "model_name");

            migrationBuilder.RenameColumn(
                name: "IsSuccess",
                table: "ai_audit_logs",
                newName: "is_success");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "ai_audit_logs",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ai_audit_logs",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CompletionTokens",
                table: "ai_audit_logs",
                newName: "completion_tokens");

            migrationBuilder.RenameIndex(
                name: "IX_ai_audit_logs_TenantId_CreatedAt",
                table: "ai_audit_logs",
                newName: "ix_ai_audit_logs_tenant_id_created_at");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tenants",
                table: "tenants",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_student_answers",
                table: "student_answers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_questions",
                table: "questions",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_exam_questions",
                table: "exam_questions",
                columns: new[] { "exam_paper_id", "question_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_exam_papers",
                table: "exam_papers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_ai_audit_logs",
                table: "ai_audit_logs",
                column: "id");

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_name = table.Column<string>(type: "text", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_name = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    attached_question_ids = table.Column<string>(type: "text", nullable: true),
                    attached_exam_paper_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id_username",
                table: "users",
                columns: new[] { "tenant_id", "username" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_exam_papers_tenants_tenant_id",
                table: "exam_papers",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_exam_questions_exam_papers_exam_paper_id",
                table: "exam_questions",
                column: "exam_paper_id",
                principalTable: "exam_papers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_exam_questions_questions_question_id",
                table: "exam_questions",
                column: "question_id",
                principalTable: "questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_questions_tenants_tenant_id",
                table: "questions",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_student_answers_exam_papers_exam_paper_id",
                table: "student_answers",
                column: "exam_paper_id",
                principalTable: "exam_papers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_student_answers_questions_question_id",
                table: "student_answers",
                column: "question_id",
                principalTable: "questions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_exam_papers_tenants_tenant_id",
                table: "exam_papers");

            migrationBuilder.DropForeignKey(
                name: "fk_exam_questions_exam_papers_exam_paper_id",
                table: "exam_questions");

            migrationBuilder.DropForeignKey(
                name: "fk_exam_questions_questions_question_id",
                table: "exam_questions");

            migrationBuilder.DropForeignKey(
                name: "fk_questions_tenants_tenant_id",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "fk_student_answers_exam_papers_exam_paper_id",
                table: "student_answers");

            migrationBuilder.DropForeignKey(
                name: "fk_student_answers_questions_question_id",
                table: "student_answers");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tenants",
                table: "tenants");

            migrationBuilder.DropPrimaryKey(
                name: "pk_student_answers",
                table: "student_answers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_questions",
                table: "questions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_exam_questions",
                table: "exam_questions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_exam_papers",
                table: "exam_papers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_ai_audit_logs",
                table: "ai_audit_logs");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "tenants",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "tenants",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "tenants",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "schema_name",
                table: "tenants",
                newName: "SchemaName");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "tenants",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "tenants",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "contact_email",
                table: "tenants",
                newName: "ContactEmail");

            migrationBuilder.RenameColumn(
                name: "ai_call_used",
                table: "tenants",
                newName: "AiCallUsed");

            migrationBuilder.RenameColumn(
                name: "ai_call_quota",
                table: "tenants",
                newName: "AiCallQuota");

            migrationBuilder.RenameIndex(
                name: "ix_tenants_schema_name",
                table: "tenants",
                newName: "IX_tenants_SchemaName");

            migrationBuilder.RenameColumn(
                name: "score",
                table: "student_answers",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "student_answers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "student_answers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "submitted_at",
                table: "student_answers",
                newName: "SubmittedAt");

            migrationBuilder.RenameColumn(
                name: "student_id",
                table: "student_answers",
                newName: "StudentId");

            migrationBuilder.RenameColumn(
                name: "question_id",
                table: "student_answers",
                newName: "QuestionId");

            migrationBuilder.RenameColumn(
                name: "grading_status",
                table: "student_answers",
                newName: "GradingStatus");

            migrationBuilder.RenameColumn(
                name: "exam_paper_id",
                table: "student_answers",
                newName: "ExamPaperId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "student_answers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "answer_content",
                table: "student_answers",
                newName: "AnswerContent");

            migrationBuilder.RenameColumn(
                name: "ai_feedback",
                table: "student_answers",
                newName: "AiFeedback");

            migrationBuilder.RenameIndex(
                name: "ix_student_answers_question_id",
                table: "student_answers",
                newName: "IX_student_answers_QuestionId");

            migrationBuilder.RenameIndex(
                name: "ix_student_answers_exam_paper_id_student_id_question_id",
                table: "student_answers",
                newName: "IX_student_answers_ExamPaperId_StudentId_QuestionId");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "questions",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "options",
                table: "questions",
                newName: "Options");

            migrationBuilder.RenameColumn(
                name: "explanation",
                table: "questions",
                newName: "Explanation");

            migrationBuilder.RenameColumn(
                name: "embedding",
                table: "questions",
                newName: "Embedding");

            migrationBuilder.RenameColumn(
                name: "difficulty",
                table: "questions",
                newName: "Difficulty");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "questions",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "questions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "questions",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "questions",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "knowledge_point",
                table: "questions",
                newName: "KnowledgePoint");

            migrationBuilder.RenameColumn(
                name: "is_ai_generated",
                table: "questions",
                newName: "IsAiGenerated");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "questions",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "questions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "correct_answer",
                table: "questions",
                newName: "CorrectAnswer");

            migrationBuilder.RenameIndex(
                name: "ix_questions_tenant_id_is_active",
                table: "questions",
                newName: "IX_questions_TenantId_IsActive");

            migrationBuilder.RenameColumn(
                name: "score",
                table: "exam_questions",
                newName: "Score");

            migrationBuilder.RenameColumn(
                name: "order",
                table: "exam_questions",
                newName: "Order");

            migrationBuilder.RenameColumn(
                name: "question_id",
                table: "exam_questions",
                newName: "QuestionId");

            migrationBuilder.RenameColumn(
                name: "exam_paper_id",
                table: "exam_questions",
                newName: "ExamPaperId");

            migrationBuilder.RenameIndex(
                name: "ix_exam_questions_question_id",
                table: "exam_questions",
                newName: "IX_exam_questions_QuestionId");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "exam_papers",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "exam_papers",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "exam_papers",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "exam_papers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "exam_papers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "total_score",
                table: "exam_papers",
                newName: "TotalScore");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "exam_papers",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "exam_papers",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "end_time",
                table: "exam_papers",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "duration_minutes",
                table: "exam_papers",
                newName: "DurationMinutes");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "exam_papers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "anti_cheating_enabled",
                table: "exam_papers",
                newName: "AntiCheatingEnabled");

            migrationBuilder.RenameIndex(
                name: "ix_exam_papers_tenant_id",
                table: "exam_papers",
                newName: "IX_exam_papers_TenantId");

            migrationBuilder.RenameColumn(
                name: "operation",
                table: "ai_audit_logs",
                newName: "Operation");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ai_audit_logs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "ai_audit_logs",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "total_tokens",
                table: "ai_audit_logs",
                newName: "TotalTokens");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "ai_audit_logs",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "related_entity_id",
                table: "ai_audit_logs",
                newName: "RelatedEntityId");

            migrationBuilder.RenameColumn(
                name: "prompt_tokens",
                table: "ai_audit_logs",
                newName: "PromptTokens");

            migrationBuilder.RenameColumn(
                name: "model_name",
                table: "ai_audit_logs",
                newName: "ModelName");

            migrationBuilder.RenameColumn(
                name: "is_success",
                table: "ai_audit_logs",
                newName: "IsSuccess");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "ai_audit_logs",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ai_audit_logs",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "completion_tokens",
                table: "ai_audit_logs",
                newName: "CompletionTokens");

            migrationBuilder.RenameIndex(
                name: "ix_ai_audit_logs_tenant_id_created_at",
                table: "ai_audit_logs",
                newName: "IX_ai_audit_logs_TenantId_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tenants",
                table: "tenants",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_student_answers",
                table: "student_answers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_questions",
                table: "questions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_exam_questions",
                table: "exam_questions",
                columns: new[] { "ExamPaperId", "QuestionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_exam_papers",
                table: "exam_papers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ai_audit_logs",
                table: "ai_audit_logs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_exam_papers_tenants_TenantId",
                table: "exam_papers",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_questions_exam_papers_ExamPaperId",
                table: "exam_questions",
                column: "ExamPaperId",
                principalTable: "exam_papers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_questions_questions_QuestionId",
                table: "exam_questions",
                column: "QuestionId",
                principalTable: "questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_questions_tenants_TenantId",
                table: "questions",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_student_answers_exam_papers_ExamPaperId",
                table: "student_answers",
                column: "ExamPaperId",
                principalTable: "exam_papers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_student_answers_questions_QuestionId",
                table: "student_answers",
                column: "QuestionId",
                principalTable: "questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
