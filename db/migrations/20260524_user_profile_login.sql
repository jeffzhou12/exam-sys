-- 用户表扩展：支持昵称、头像、手机号、微信标识和常规资料字段
ALTER TABLE users
    ADD COLUMN IF NOT EXISTS nickname       VARCHAR(100),
    ADD COLUMN IF NOT EXISTS avatar_url     VARCHAR(1000),
    ADD COLUMN IF NOT EXISTS phone_number    VARCHAR(30),
    ADD COLUMN IF NOT EXISTS wechat_openid   VARCHAR(100),
    ADD COLUMN IF NOT EXISTS wechat_unionid  VARCHAR(100),
    ADD COLUMN IF NOT EXISTS gender          VARCHAR(20),
    ADD COLUMN IF NOT EXISTS address         VARCHAR(500);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_tenant_email
    ON users(tenant_id, email)
    WHERE email IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_tenant_phone
    ON users(tenant_id, phone_number)
    WHERE phone_number IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_tenant_wechat_id
    ON users(tenant_id, wechat_openid)
    WHERE wechat_openid IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_tenant_wechat_uid
    ON users(tenant_id, wechat_unionid)
    WHERE wechat_unionid IS NOT NULL;