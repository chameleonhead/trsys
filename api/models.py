from django.db import models


class OrderType(models.TextChoices):
    BUY = 'BUY'
    SELL = 'SELL'


class Order(models.Model):
    ticket_no = models.TextField()
    symbol = models.TextField()
    order_type = models.TextField(choices=OrderType.choices)

    def __str__(self) -> str:
        return f'{self.ticket_no}:{self.symbol}:{self.order_type}'
