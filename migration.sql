-- EF Core Migrations Script
-- Generated from: InitialCreate + MakeKnowledgeChecksumIndexNonUnique

CREATE DATABASE IF NOT EXISTS `demo` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `demo`;

-- Migration: InitialCreate
CREATE TABLE IF NOT EXISTS `permissions` (
    `id` char(36) NOT NULL,
    `code` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `name` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `group_name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_permissions` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `tenants` (
    `id` char(36) NOT NULL,
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `code` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `modified_at` datetime(6) NULL,
    CONSTRAINT `PK_tenants` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `users` (
    `id` char(36) NOT NULL,
    `email` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `password_hash` text CHARACTER SET utf8mb4 NOT NULL,
    `full_name` varchar(255) CHARACTER SET utf8mb4 NULL,
    `employee_code` varchar(50) CHARACTER SET utf8mb4 NULL,
    `project` varchar(255) CHARACTER SET utf8mb4 NULL,
    `job_position` varchar(255) CHARACTER SET utf8mb4 NULL,
    `status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `modified_at` datetime(6) NULL,
    CONSTRAINT `PK_users` PRIMARY KEY (`id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `roles` (
    `id` char(36) NOT NULL,
    `tenant_id` char(36) NULL,
    `name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `code` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `is_system_role` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `modified_at` datetime(6) NULL,
    CONSTRAINT `PK_roles` PRIMARY KEY (`id`),
    CONSTRAINT `FK_roles_tenants_tenant_id` FOREIGN KEY (`tenant_id`) REFERENCES `tenants` (`id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `agents` (
    `id` char(36) NOT NULL,
    `tenant_id` char(36) NULL,
    `created_by_user_id` char(36) NOT NULL,
    `modified_by_user_id` char(36) NULL,
    `code` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `description` text CHARACTER SET utf8mb4 NULL,
    `icon` varchar(500) CHARACTER SET utf8mb4 NULL,
    `scope` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `role` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `published_at` datetime(6) NULL,
    `deleted_at` datetime(6) NULL,
    `created_at` datetime(6) NOT NULL,
    `modified_at` datetime(6) NULL,
    CONSTRAINT `PK_agents` PRIMARY KEY (`id`),
    CONSTRAINT `FK_agents_tenants_tenant_id` FOREIGN KEY (`tenant_id`) REFERENCES `tenants` (`id`),
    CONSTRAINT `FK_agents_users_created_by_user_id` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_agents_users_modified_by_user_id` FOREIGN KEY (`modified_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `audit_logs` (
    `id` char(36) NOT NULL,
    `user_id` char(36) NULL,
    `tenant_id` char(36) NULL,
    `action` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `user_name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `target_type` varchar(100) CHARACTER SET utf8mb4 NULL,
    `target_id` varchar(100) CHARACTER SET utf8mb4 NULL,
    `ip_address` varchar(100) CHARACTER SET utf8mb4 NULL,
    `description` text CHARACTER SET utf8mb4 NOT NULL,
    `created_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_audit_logs` PRIMARY KEY (`id`),
    CONSTRAINT `FK_audit_logs_tenants_tenant_id` FOREIGN KEY (`tenant_id`) REFERENCES `tenants` (`id`) ON DELETE SET NULL,
    CONSTRAINT `FK_audit_logs_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `knowledge_storage_objects` (
    `id` char(36) NOT NULL,
    `storage_bucket` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `storage_object_key` varchar(643) CHARACTER SET utf8mb4 NOT NULL,
    `storage_etag` varchar(255) CHARACTER SET utf8mb4 NULL,
    `storage_version_id` varchar(255) CHARACTER SET utf8mb4 NULL,
    `checksum_sha256` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `size_bytes` bigint NOT NULL,
    `content_type` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `created_by_user_id` char(36) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `deleted_at` datetime(6) NULL,
    CONSTRAINT `PK_knowledge_storage_objects` PRIMARY KEY (`id`),
    CONSTRAINT `FK_knowledge_storage_objects_users_created_by_user_id` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `user_sessions` (
    `id` char(36) NOT NULL,
    `user_id` char(36) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `expires_at` datetime(6) NOT NULL,
    `revoked_at` datetime(6) NULL,
    `created_by_ip` varchar(100) CHARACTER SET utf8mb4 NULL,
    `revoked_by_ip` varchar(100) CHARACTER SET utf8mb4 NULL,
    `reason_revoked` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_user_sessions` PRIMARY KEY (`id`),
    CONSTRAINT `FK_user_sessions_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `user_tenants` (
    `id` char(36) NOT NULL,
    `user_id` char(36) NOT NULL,
    `tenant_id` char(36) NOT NULL,
    `status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `modified_at` datetime(6) NULL,
    CONSTRAINT `PK_user_tenants` PRIMARY KEY (`id`),
    CONSTRAINT `FK_user_tenants_tenants_tenant_id` FOREIGN KEY (`tenant_id`) REFERENCES `tenants` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_user_tenants_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `role_permissions` (
    `id` char(36) NOT NULL,
    `role_id` char(36) NOT NULL,
    `permission_id` char(36) NOT NULL,
    CONSTRAINT `PK_role_permissions` PRIMARY KEY (`id`),
    CONSTRAINT `FK_role_permissions_permissions_permission_id` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_role_permissions_roles_role_id` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `user_roles` (
    `id` char(36) NOT NULL,
    `user_id` char(36) NOT NULL,
    `role_id` char(36) NOT NULL,
    `tenant_id` char(36) NULL,
    `assigned_at` datetime(6) NULL,
    `created_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_user_roles` PRIMARY KEY (`id`),
    CONSTRAINT `FK_user_roles_roles_role_id` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_user_roles_tenants_tenant_id` FOREIGN KEY (`tenant_id`) REFERENCES `tenants` (`id`),
    CONSTRAINT `FK_user_roles_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `agent_knowledge_folders` (
    `id` char(36) NOT NULL,
    `agent_id` char(36) NOT NULL,
    `parent_folder_id` char(36) NULL,
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `normalized_name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `created_by_user_id` char(36) NOT NULL,
    `modified_by_user_id` char(36) NULL,
    `created_at` datetime(6) NOT NULL,
    `modified_at` datetime(6) NULL,
    `deleted_at` datetime(6) NULL,
    CONSTRAINT `PK_agent_knowledge_folders` PRIMARY KEY (`id`),
    CONSTRAINT `FK_agent_knowledge_folders_agent_knowledge_folders_parent_folde~` FOREIGN KEY (`parent_folder_id`) REFERENCES `agent_knowledge_folders` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_agent_knowledge_folders_agents_agent_id` FOREIGN KEY (`agent_id`) REFERENCES `agents` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_agent_knowledge_folders_users_created_by_user_id` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_agent_knowledge_folders_users_modified_by_user_id` FOREIGN KEY (`modified_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `refresh_tokens` (
    `id` char(36) NOT NULL,
    `user_id` char(36) NOT NULL,
    `session_id` char(36) NOT NULL,
    `token_hash` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `expires_at` datetime(6) NOT NULL,
    `revoked_at` datetime(6) NULL,
    `replaced_by_token_hash` varchar(255) CHARACTER SET utf8mb4 NULL,
    `created_at` datetime(6) NOT NULL,
    `created_by_ip` varchar(100) CHARACTER SET utf8mb4 NULL,
    `revoked_by_ip` varchar(100) CHARACTER SET utf8mb4 NULL,
    `reason_revoked` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_refresh_tokens` PRIMARY KEY (`id`),
    CONSTRAINT `FK_refresh_tokens_user_sessions_session_id` FOREIGN KEY (`session_id`) REFERENCES `user_sessions` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_refresh_tokens_users_user_id` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `agent_knowledge_files` (
    `id` char(36) NOT NULL,
    `agent_id` char(36) NOT NULL,
    `folder_id` char(36) NULL,
    `storage_object_id` char(36) NOT NULL,
    `name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `normalized_name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `original_name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `extension` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `created_by_user_id` char(36) NOT NULL,
    `modified_by_user_id` char(36) NULL,
    `created_at` datetime(6) NOT NULL,
    `modified_at` datetime(6) NULL,
    `deleted_at` datetime(6) NULL,
    CONSTRAINT `PK_agent_knowledge_files` PRIMARY KEY (`id`),
    CONSTRAINT `FK_agent_knowledge_files_agent_knowledge_folders_folder_id` FOREIGN KEY (`folder_id`) REFERENCES `agent_knowledge_folders` (`id`) ON DELETE SET NULL,
    CONSTRAINT `FK_agent_knowledge_files_agents_agent_id` FOREIGN KEY (`agent_id`) REFERENCES `agents` (`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_agent_knowledge_files_knowledge_storage_objects_storage_obje~` FOREIGN KEY (`storage_object_id`) REFERENCES `knowledge_storage_objects` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_agent_knowledge_files_users_created_by_user_id` FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_agent_knowledge_files_users_modified_by_user_id` FOREIGN KEY (`modified_by_user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_agent_knowledge_files_agent_id_created_at` ON `agent_knowledge_files` (`agent_id`, `created_at`);
CREATE INDEX `IX_agent_knowledge_files_agent_id_created_by_user_id` ON `agent_knowledge_files` (`agent_id`, `created_by_user_id`);
CREATE INDEX `IX_agent_knowledge_files_agent_id_folder_id` ON `agent_knowledge_files` (`agent_id`, `folder_id`);
CREATE INDEX `IX_agent_knowledge_files_agent_id_normalized_name` ON `agent_knowledge_files` (`agent_id`, `normalized_name`);
CREATE INDEX `IX_agent_knowledge_files_created_by_user_id` ON `agent_knowledge_files` (`created_by_user_id`);
CREATE INDEX `IX_agent_knowledge_files_folder_id` ON `agent_knowledge_files` (`folder_id`);
CREATE INDEX `IX_agent_knowledge_files_modified_by_user_id` ON `agent_knowledge_files` (`modified_by_user_id`);
CREATE INDEX `IX_agent_knowledge_files_storage_object_id` ON `agent_knowledge_files` (`storage_object_id`);
CREATE INDEX `IX_agent_knowledge_folders_agent_id_normalized_name` ON `agent_knowledge_folders` (`agent_id`, `normalized_name`);
CREATE INDEX `IX_agent_knowledge_folders_agent_id_parent_folder_id` ON `agent_knowledge_folders` (`agent_id`, `parent_folder_id`);
CREATE INDEX `IX_agent_knowledge_folders_created_by_user_id` ON `agent_knowledge_folders` (`created_by_user_id`);
CREATE INDEX `IX_agent_knowledge_folders_modified_by_user_id` ON `agent_knowledge_folders` (`modified_by_user_id`);
CREATE INDEX `IX_agent_knowledge_folders_parent_folder_id` ON `agent_knowledge_folders` (`parent_folder_id`);
CREATE INDEX `IX_agents_created_by_user_id` ON `agents` (`created_by_user_id`);
CREATE INDEX `IX_agents_modified_by_user_id` ON `agents` (`modified_by_user_id`);
CREATE INDEX `IX_agents_scope` ON `agents` (`scope`);
CREATE INDEX `IX_agents_tenant_id` ON `agents` (`tenant_id`);
CREATE UNIQUE INDEX `IX_agents_tenant_id_code` ON `agents` (`tenant_id`, `code`);
CREATE INDEX `IX_audit_logs_action` ON `audit_logs` (`action`);
CREATE INDEX `IX_audit_logs_created_at` ON `audit_logs` (`created_at`);
CREATE INDEX `IX_audit_logs_tenant_id` ON `audit_logs` (`tenant_id`);
CREATE INDEX `IX_audit_logs_user_id` ON `audit_logs` (`user_id`);
CREATE INDEX `IX_knowledge_storage_objects_checksum_sha256_size_bytes` ON `knowledge_storage_objects` (`checksum_sha256`, `size_bytes`);
CREATE INDEX `IX_knowledge_storage_objects_created_by_user_id` ON `knowledge_storage_objects` (`created_by_user_id`);
CREATE UNIQUE INDEX `IX_knowledge_storage_objects_storage_bucket_storage_object_key` ON `knowledge_storage_objects` (`storage_bucket`, `storage_object_key`);
CREATE UNIQUE INDEX `IX_permissions_code` ON `permissions` (`code`);
CREATE INDEX `IX_refresh_tokens_session_id` ON `refresh_tokens` (`session_id`);
CREATE UNIQUE INDEX `IX_refresh_tokens_token_hash` ON `refresh_tokens` (`token_hash`);
CREATE INDEX `IX_refresh_tokens_user_id` ON `refresh_tokens` (`user_id`);
CREATE INDEX `IX_role_permissions_permission_id` ON `role_permissions` (`permission_id`);
CREATE INDEX `IX_role_permissions_role_id` ON `role_permissions` (`role_id`);
CREATE UNIQUE INDEX `IX_role_permissions_role_id_permission_id` ON `role_permissions` (`role_id`, `permission_id`);
CREATE INDEX `IX_roles_tenant_id` ON `roles` (`tenant_id`);
CREATE UNIQUE INDEX `IX_roles_tenant_id_code` ON `roles` (`tenant_id`, `code`);
CREATE UNIQUE INDEX `IX_tenants_code` ON `tenants` (`code`);
CREATE INDEX `IX_user_roles_role_id` ON `user_roles` (`role_id`);
CREATE INDEX `IX_user_roles_tenant_id` ON `user_roles` (`tenant_id`);
CREATE INDEX `IX_user_roles_user_id` ON `user_roles` (`user_id`);
CREATE UNIQUE INDEX `IX_user_roles_user_id_role_id_tenant_id` ON `user_roles` (`user_id`, `role_id`, `tenant_id`);
CREATE INDEX `IX_user_sessions_expires_at` ON `user_sessions` (`expires_at`);
CREATE INDEX `IX_user_sessions_user_id` ON `user_sessions` (`user_id`);
CREATE INDEX `IX_user_tenants_tenant_id` ON `user_tenants` (`tenant_id`);
CREATE INDEX `IX_user_tenants_user_id` ON `user_tenants` (`user_id`);
CREATE UNIQUE INDEX `IX_user_tenants_user_id_tenant_id` ON `user_tenants` (`user_id`, `tenant_id`);
CREATE UNIQUE INDEX `IX_users_email` ON `users` (`email`);

-- Migration history
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260623091346_InitialCreate', '9.0.5'),
       ('20260624172000_MakeKnowledgeChecksumIndexNonUnique', '9.0.5');
