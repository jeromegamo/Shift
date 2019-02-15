alter table Player
add HealthPoints int not null,
    ManaPoints int not null,
    Gold decimal not null,
    constraint DF_HealthPoints default 100 for HealthPoints,
    constraint DF_ManaPoints default 50 for ManaPoints,
    constraint DF_Gold default 0 for Gold