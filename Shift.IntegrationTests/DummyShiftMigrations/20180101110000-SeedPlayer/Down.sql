alter table Inventory
drop constraint [FK_Player_Inventory]

truncate table Player

alter table Inventory
add constraint [FK_Player_Inventory] foreign key (PlayerId)
    references Player(Id)