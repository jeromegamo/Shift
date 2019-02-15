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