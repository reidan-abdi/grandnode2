db = db.getSiblingDB('grandnodedb2');
db.createUser({
    user: 'root',
    pwd: 'example',
    roles: [
        { role: 'readWrite', db: 'grandnodedb2' },
        { role: 'dbAdmin', db: 'grandnodedb2' }
    ]
});
print('✅ User created in grandnodedb2');
