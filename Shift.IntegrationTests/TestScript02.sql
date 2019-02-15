create table Player(
    Id bigint identity(1,1),
    Name varchar(50) not null,
    constraint PK_Player primary key (Id)
)

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