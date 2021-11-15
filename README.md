# Computer.Bus

## feeds
nuget add ~\source\repos\Computer.Bus\source\Computer.Bus.Contracts\bin\Debug\Computer.Bus.Contracts.0.0.1.nupkg -source ~\source\repos\Feeds\Computer\
nuget add ~\source\repos\Computer.Bus\source\Computer.Bus.RabbitMq\bin\Debug\Computer.Bus.RabbitMq.0.0.2.nupkg -source ~\source\repos\Feeds\Computer\

## RabbitMq
docker run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.9-management