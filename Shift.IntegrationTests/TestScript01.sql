create table Player(
    Id bigint identity(1,1),
    Name varchar(50) not null,
    constraint PK_Player primary key (Id)
)

alter table Player
add HealthPoints int not null,
    ManaPoints int not null,
    Gold decimal not null,
    constraint DF_HealthPoints default 100 for HealthPoints,
    constraint DF_ManaPoints default 50 for ManaPoints,
    constraint DF_Gold default 0 for Gold

create table ItemCategory(
    Name varchar(50) not null,
    constraint PK_ItemCategory primary key (Name)
)

create table Item(
    Id bigint identity(1,1),
    Name varchar(50) not null,
    Category varchar(50) not null,
    constraint PK_Item primary key (Id),
    constraint FK_ItemCategory_Item foreign key (Category)
    references ItemCategory(Name)
)

create table Inventory(
    Id bigint identity(1,1),
    PlayerId bigint not null,
    ItemId bigint not null,
    Quantity int not null,
    constraint PK_Inventory primary key (Id),
    constraint FK_Player_Inventory foreign key (PlayerId)
    references Player(Id),
    constraint FK_Item_Inventory foreign key (ItemId)
    references Item(Id)
)

insert into ItemCategory(Name)
values ('Weapon'),('Armor'),('Consumable')

insert into Player(Name)
values ('Legolas123'),('Kirito'),('Rimuru Tempest')