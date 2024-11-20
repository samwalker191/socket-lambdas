## AWS Architecture

[AWS Architecture](readme-assets/aws-arch.png)

## Design Choices

The main design choices were the database and how to structure the AWS Lambda functions.

MongoDB was chosen as the database to persist connections and subscriptions to due to its 
efficient read and write operations since no joins or $lookups are needed. Additionally,
its flexibility in the data being stored was a factor. I was considering going with the AWS
recommended database of DynamoDB, but found that if I wanted to store a list of subscriptions
then it would lose out on some of its benefits due to the need to scan. Since I would be
storing a list on all the documents and would need to search for specific string within that list
I went with MongoDB since it allows for an index on a list field. Finally, because of the time
limit of this project, I chose to go with MongoDB due to my familiarity with it and its use in
C#. 

NOTE: As stated above, an index was created for the Subscriptions field of the mongo document. 
Additionally, an index was created on the `connectionId` field in order to improve query performance
since the code is executing queries to find a connection by the `connectionId` often.

I also chose to separate out the functionality described in the project into two separate
Lambda functions. Specifically, a Subscribe function and a Publish function. I chose to do
this because I believe it made the organization of the codebase to be more manageable. If
the functionality of one needed to change, it is easy to find and change that within the code
base. It also makes the logic within the function to be focused on performing one task making it
quicker to complete.

## Infrastructure
This repository uses the AWS Lambda Tooling which allows for the deployment of serverless
lambda functions using the `template.yml` file to describe the settings. You would just need to
run this command, `dotnet lambda deploy-serverless`. This uses some defaults defined in
`aws-lambda-tools-defaults.json`. These tools use AWS CloudFormation to deploy and update the functions.
Some CI/CD would still need to be put in place that can use this function as well 
as automatically running the test suites before attempting to deploy.

Logging from the Lambda functions is set up to output into CloudWatch. Some alerts could be set
up around errors or failures to notify developers of something going wrong in a more proactive way.

## Re:Build Principals
I believe that Principal 05 is the one that would most strongly enhance my work and my growth.

```We listen carefully and non-defensively to one another, customers, suppliers, and community members.```

It is incredibly important to listen to others with empathy and take the effort to be non-defensive.
If energy is spent on being defensive then that is energy and focus not being spent on understanding
the other person and learning from them. I feel that understanding other people and their thought process
helps improve my own and broadens my perspectives in not just my work, but in my personal life as well. This 
principal really resonates with me because of that.

